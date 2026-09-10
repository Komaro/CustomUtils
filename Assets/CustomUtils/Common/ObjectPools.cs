using System;
using System.Collections.Generic;
using System.Text;

public static class ObjectPools {

    #region [StringBuilder]

    public static readonly LocalObjectPool<StringBuilder> StringBuilderPool = new(OnCreateStringBuilder, onRelease:OnReleaseStringBuilder);

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

public sealed class LocalObjectPool<T> : IDisposable where T : class {
    
    private readonly Func<T> _onCreate;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDestroy;
    private readonly Stack<T> _stack = new();

    private bool _isDisposed;

    public int MaxSize { get; }

    private const int DEFAULT_MAX_SIZE = 10;

    public LocalObjectPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int maxSize = DEFAULT_MAX_SIZE) {
        _onCreate = onCreate ?? throw new ArgumentNullException(nameof(onCreate));
        _onGet = onGet;
        _onRelease = onRelease;
        _onDestroy = onDestroy;

        MaxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "Pool max size must be greater than zero.");
    }

    public PoolLease<T> Get(out T obj) => new(this, obj = Get());

    public T Get() {
        ThrowIfDisposed();

        var obj = _stack.Count > 0 ? _stack.Pop() : _onCreate() ?? throw new InvalidOperationException($"{nameof(_onCreate)} returned null for {typeof(T).Name}.");
        try {
            _onGet?.Invoke(obj);
        } catch (Exception exception) {
            try {
                Destroy(obj);
            } catch (Exception destroyException) {
                throw new AggregateException(exception, destroyException);
            }

            throw;
        }

        return obj;
    }

    public void Release(T pooledObject) {
        if (pooledObject == null) {
            throw new ArgumentNullException(nameof(pooledObject));
        }

        try {
            _onRelease?.Invoke(pooledObject);
        } catch (Exception exception) {
            try {
                Destroy(pooledObject);
            } catch (Exception destroyException) {
                throw new AggregateException(exception, destroyException);
            }

            throw;
        }

        if (_isDisposed || _stack.Count >= MaxSize) {
            Destroy(pooledObject);
            return;
        }

        _stack.Push(pooledObject);
    }

    public void Dispose() {
        if (_isDisposed)
            return;

        _isDisposed = true;
        List<Exception> exceptions = null;

        while (_stack.Count > 0) {
            try {
                Destroy(_stack.Pop());
            } catch (Exception exception) {
                exceptions ??= new List<Exception>();
                exceptions.Add(exception);
            }
        }

        if (exceptions is { Count: > 0 }) {
            throw new AggregateException($"Failed to destroy one or more pooled {typeof(T).Name} instances.", exceptions);
        }
    }

    private void Destroy(T obj) {
        if (_onDestroy != null) {
            _onDestroy.Invoke(obj);
        } else if (obj is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed() {
        if (_isDisposed) {
            throw new ObjectDisposedException(nameof(LocalObjectPool<T>));
        }
    }
}

public readonly struct PoolLease<T> : IDisposable where T : class {
    
    private readonly LocalObjectPool<T> pool;
    private readonly T pooledObject;

    public PoolLease(LocalObjectPool<T> pool, T pooledObject) {
        this.pool = pool;
        this.pooledObject = pooledObject;
    }

    public void Dispose() => pool?.Release(pooledObject);
}