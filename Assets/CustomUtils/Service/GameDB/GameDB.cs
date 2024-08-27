using System;
using System.Collections.Concurrent;

public abstract class GameDB<TKey, TData> : IDisposable {

    private readonly ConcurrentDictionary<TKey, TData> _dictionary = new();

    public TData this[TKey index] => _dictionary.TryGetValue(index, out var data) ? data : default;
    
    public int Length => _dictionary?.Count ?? 0;
    public int Count => _dictionary?.Count ?? 0;
    
    public GameDB(GameDBProvider provider) => Init(provider);
    
    ~GameDB() => Dispose();
    public void Dispose() => _dictionary.Clear();
    
    private void Init(GameDBProvider provider) { 
        foreach (var data in provider.GetDataList<TData>()) {
            var key = CreateKey(data);
            if (_dictionary.TryAdd(key, data) == false) {
                Logger.TraceError($"Duplicate key value || {key} ");
            }
        }
    }

    public bool TryGet(TKey key, out TData data) => (data = Get(key)) != null;
    
    public virtual TData Get(TKey key) {
        if (_dictionary.TryGetValue(key, out var value)) {
            return value;
        }
        
        return default;
    }
    
    protected abstract TKey CreateKey(TData data);
}