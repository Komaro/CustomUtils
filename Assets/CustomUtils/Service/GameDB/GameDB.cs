using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

public abstract class GameDB<TKey, TData> : IEnumerable<KeyValuePair<TKey, TData>>, IDisposable {

    private readonly ConcurrentDictionary<TKey, TData> _dictionary = new();

    public TData this[TKey index] => _dictionary.GetValueOrDefault(index);
    
    public int Length => _dictionary?.Count ?? 0;
    public int Count => _dictionary?.Count ?? 0;

    private bool _disposed = false;
    
    public GameDB(GameDBProvider provider) => Init(provider);

    ~GameDB() {
        if (_disposed == false) {
            Dispose();
        }
    }

    public void Dispose() {
        if (_disposed == false) {
            _dictionary.Clear();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }

    private void Init(GameDBProvider provider) { 
        foreach (var data in provider.GetData<TData>()) {
            var key = CreateKey(data);
            if (_dictionary.TryAdd(key, data) == false) {
                Logger.TraceError($"Duplicate key value || {key} ");
            }
        }
    }

    public bool TryGet(TKey key, out TData data) => (data = Get(key)) != null;
    public virtual TData Get(TKey key) => _dictionary.GetValueOrDefault(key);

    protected abstract TKey CreateKey(TData data);
    
    public IEnumerator<KeyValuePair<TKey, TData>> GetEnumerator() => _dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}