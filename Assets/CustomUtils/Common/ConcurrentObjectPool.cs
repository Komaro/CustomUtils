using System;
using UnityEngine.Pool;

public class ConcurrentObjectPool<T> : ObjectPool<T> where T : class {
    
    public ConcurrentObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000) : base(createFunc, actionOnGet, actionOnRelease, actionOnDestroy, collectionCheck, defaultCapacity, maxSize) { }

    private object _lock = new();

    public new PooledObject<T> Get(out T obj) {
        lock (_lock) {
            return base.Get(out obj);
        }
    }

    public new T Get() {
        lock (_lock) {
            return base.Get();
        }
    }

    public new void Release(T obj) {
        lock (_lock) {
            base.Release(obj);
        }
    }
}