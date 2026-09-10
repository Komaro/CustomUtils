using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public static class ThreadSafeObjectPools {

    #region [StringBuilder]

    public static readonly ThreadSafeObjectPool<StringBuilder> StringBuilderPool = new(OnCreateStringBuilder, onRelease:OnReleaseStringBuilder);

    private const int MAX_CAPACITY = 1024;
    private const int DEFAULT_CAPACITY = 256;

    private static StringBuilder OnCreateStringBuilder() {
        var stringBuilder = new StringBuilder();
        stringBuilder.EnsureCapacity(DEFAULT_CAPACITY);
        return stringBuilder;
    }

    private static void OnReleaseStringBuilder(StringBuilder stringBuilder) {
        stringBuilder.Clear();
        if (stringBuilder.Capacity > MAX_CAPACITY) {
            stringBuilder.Capacity = DEFAULT_CAPACITY;
        }
    }

    #endregion
}


public sealed class ThreadSafeObjectPool<T> : IDisposable where T : class {
    
    private readonly Func<T> _onCreate;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDestroy;
    private readonly ConcurrentBag<Entry> _bag = new();

    private int _inactiveCount;
    private int _disposeState;

    public int MaxSize { get; }
    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private const int DEFAULT_MAX_SIZE = 10;

    public ThreadSafeObjectPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int maxSize = DEFAULT_MAX_SIZE) {
        _onCreate = onCreate ?? throw new ArgumentNullException(nameof(onCreate));
        _onGet = onGet;
        _onRelease = onRelease;
        _onDestroy = onDestroy;

        MaxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "Pool max size must be greater than zero.");
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) {
            return;
        }

        List<Exception> exceptions = null;
        while (_bag.TryTake(out var entry)) {
            Interlocked.Decrement(ref _inactiveCount);

            try {
                Destroy(entry.value);
            } catch (Exception exception) {
                exceptions ??= new List<Exception>();
                exceptions.Add(exception);
            }
        }

        if (exceptions is { Count: > 0 }) {
            throw new AggregateException($"Failed to destroy one or more pooled {typeof(T).Name} instances.", exceptions);
        }
    }

    public ThreadSafePoolLease<T> Rent() => Rent(out _);

    public ThreadSafePoolLease<T> Rent(out T obj) {
        ThrowIfDisposed();

        if (_bag.TryTake(out var entry)) {
            Interlocked.Decrement(ref _inactiveCount);
        } else {
            entry = new Entry(_onCreate() ?? throw new InvalidOperationException($"{nameof(_onCreate)} returned null for {typeof(T).Name}."));
        }

        var leaseToken = Interlocked.Increment(ref entry.leaseState);
        if ((leaseToken & 1L) == 0L) {
            Destroy(entry.value);
            throw new InvalidOperationException($"Invalid lease state for pooled {typeof(T).Name} instance.");
        }

        try {
            _onGet?.Invoke(entry.value);
        }catch (Exception exception) {
            Interlocked.CompareExchange(ref entry.leaseState, unchecked(leaseToken + 1L), leaseToken);

            try {
                Destroy(entry.value);
            } catch (Exception destroyException) {
                throw new AggregateException(exception, destroyException);
            }

            throw;
        }

        obj = entry.value;
        return new ThreadSafePoolLease<T>(this, entry, leaseToken);
    }

    public bool Return(in ThreadSafePoolLease<T> poolLease) {
        if (ReferenceEquals(poolLease.pool, this) == false || poolLease.entry == null || (poolLease.leaseToken & 1L) == 0L) {
            return false;
        }

        var entry = poolLease.entry;
        var returnedState = unchecked(poolLease.leaseToken + 1L);
        if (Interlocked.CompareExchange(ref entry.leaseState, returnedState, poolLease.leaseToken) != poolLease.leaseToken)
            return false;

        try {
            _onRelease?.Invoke(entry.value);
        } catch (Exception exception) {
            try {
                Destroy(entry.value);
            } catch (Exception destroyException) {
                throw new AggregateException(exception, destroyException);
            }

            throw;
        }

        if (IsDisposed) {
            Destroy(entry.value);
            return true;
        }

        if (Interlocked.Increment(ref _inactiveCount) > MaxSize) {
            Interlocked.Decrement(ref _inactiveCount);
            Destroy(entry.value);
            return true;
        }

        _bag.Add(entry);
        if (IsDisposed && _bag.TryTake(out var disposedEntry)) {
            Interlocked.Decrement(ref _inactiveCount);
            Destroy(disposedEntry.value);
        }

        return true;
    }

    private void Destroy(T obj) {
        if (_onDestroy != null) {
            _onDestroy.Invoke(obj);
        } else if (obj is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed() {
        if (IsDisposed) {
            throw new ObjectDisposedException(nameof(ThreadSafeObjectPool<T>));
        }
    }

    internal sealed class Entry {
        
        internal readonly T value;
        internal long leaseState;

        internal Entry(T value) {
            this.value = value;
        }
    }
}

public readonly struct ThreadSafePoolLease<T> : IDisposable where T : class {
    
    internal readonly ThreadSafeObjectPool<T> pool;
    internal readonly ThreadSafeObjectPool<T>.Entry entry;
    internal readonly long leaseToken;

    public bool IsValid => pool != null && entry != null && Volatile.Read(ref entry.leaseState) == leaseToken;
    public T Value => IsValid ? entry.value : throw new ObjectDisposedException(nameof(ThreadSafePoolLease<T>));

    internal ThreadSafePoolLease(ThreadSafeObjectPool<T> pool, ThreadSafeObjectPool<T>.Entry entry, long leaseToken) {
        this.pool = pool;
        this.entry = entry;
        this.leaseToken = leaseToken;
    }

    public void Dispose() => pool?.Return(this);
}