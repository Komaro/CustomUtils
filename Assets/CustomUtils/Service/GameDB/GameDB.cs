using System;
using System.Collections;
using System.Collections.Generic;

public abstract class GameDB<TKey, TData> : IEnumerable<KeyValuePair<TKey, TData>>, IDisposable {
    
    protected readonly Dictionary<TKey, TData> dataDic = new();

    public Dictionary<TKey, TData>.KeyCollection Keys => dataDic.Keys;
    public Dictionary<TKey, TData>.ValueCollection Values => dataDic.Values;
        
    public TData this[TKey index] => dataDic.GetValueOrDefault(index);
    
    public int Length => dataDic?.Count ?? 0;
    public int Count => dataDic?.Count ?? 0;

    private bool _disposed = false;
    
    public GameDB(GameDBProvider provider) => Init(provider);

    ~GameDB() {
        if (_disposed == false) {
            Dispose();
        }
    }

    public void Dispose() {
        if (_disposed == false) {
            dataDic.Clear();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }

    private void Init(GameDBProvider provider) { 
        foreach (var data in provider.GetData<TData>()) {
            var key = CreateKey(data);
            if (dataDic.TryAdd(key, data) == false) {
                Logger.TraceError($"Duplicate key value || {typeof(TData).Name} || {key}");
            }
        }
    }

    public bool TryGet(TKey key, out TData data) => (data = Get(key)) != null;
    public virtual TData Get(TKey key) => dataDic.GetValueOrDefault(key);

    protected abstract TKey CreateKey(TData data);
    
    public IEnumerator<KeyValuePair<TKey, TData>> GetEnumerator() => dataDic.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}