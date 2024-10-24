using System;
using System.Collections.Generic;
using System.Collections.Specialized;

public class NotifyDictionary<TKey, TValue> : NotifyCollection<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue> {

    public ICollection<TKey> Keys => collection.Keys;
    public ICollection<TValue> Values => collection.Values;
    
    public void Add(TKey key, TValue value) {
        if (value == null) {
            throw new NullReferenceException();
        }
        
        collection.Add(key, value);
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public bool ContainsKey(TKey key) => collection.ContainsKey(key);
    
    public bool Remove(TKey key) {
        if (collection.Remove(key)) {
            OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) => collection.TryGetValue(key, out value);

    public TValue this[TKey key] {
        get => collection[key];
        set {
            collection[key] = value;
            OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
        }
    }
}