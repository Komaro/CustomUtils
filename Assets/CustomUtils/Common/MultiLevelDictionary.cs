using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public abstract class MultiLayerDictionary<TDictionary, TCollection, TKey, TValue> : IDictionary<TKey, TCollection> 
    where TDictionary : IDictionary<TKey, TCollection>, new()
    where TCollection : ICollection<TValue> {

    protected TDictionary dictionary;
    
    protected readonly bool KEY_IS_CLASS = typeof(TKey).IsClass;
    protected readonly bool VALUE_IS_CLASS = typeof(TValue).IsClass;

    protected MultiLayerDictionary() => dictionary = new TDictionary();

    public MultiLayerDictionary(TDictionary dictionary) : this() {
        foreach (var (key, value) in dictionary) {
            this.dictionary.Add(key, value);
        }
    }

    public MultiLayerDictionary(params KeyValuePair<TKey, TCollection>[] pairs) : this() {
        foreach (var (key, value) in pairs) {
            dictionary.Add(key, value);
        }
    }

    public MultiLayerDictionary(IEnumerable<KeyValuePair<TKey, TCollection>> enumerable) : this() {
        foreach (var (key, value) in enumerable) {
            dictionary.Add(key, value);
        }
    }
    
    public virtual TCollection this[TKey key] {
        get => dictionary[key];
        set => dictionary[key] = value;
    }
    
    public virtual ICollection<TKey> Keys => dictionary.Keys;
    public virtual ICollection<TCollection> Values => dictionary.Values;
    
    public IEnumerator<KeyValuePair<TKey, TCollection>> GetEnumerator() => dictionary.GetEnumerator();

    public virtual IEnumerator<TCollection> GetEnumerator(TKey key) {
        ValidateOrThrowKey(ref key);
        if (dictionary.TryGetValue(key, out var collection)) {
            yield return collection;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<KeyValuePair<TKey, TCollection>>.Add(KeyValuePair<TKey, TCollection> item) {
        ValidateOrThrowKeyValue(ref item);
        dictionary.AutoAdd(item.Key, item.Value);
    }

    public virtual void Clear() => dictionary.Clear();
    
    public virtual void Clear(TKey key) {
        ValidateOrThrowKey(ref key);
        if (dictionary.TryGetValue(key, out var collection)) {
            collection.Clear();
        }
    }

    bool ICollection<KeyValuePair<TKey, TCollection>>.Contains(KeyValuePair<TKey, TCollection> item) {
        ValidateOrThrowKeyValue(ref item);
        return dictionary.TryGetValue(item.Key, out var innerDic) && innerDic.Equals(item.Value);
    }

    void ICollection<KeyValuePair<TKey, TCollection>>.CopyTo(KeyValuePair<TKey, TCollection>[] array, int arrayIndex) => dictionary.ToList().CopyTo(array, arrayIndex);
    
    bool ICollection<KeyValuePair<TKey, TCollection>>.Remove(KeyValuePair<TKey, TCollection> item) {
        ValidateOrThrowKeyValue(ref item);
        return dictionary.Remove(item.Key);
    }

    public virtual int Count => dictionary.Count;
    public virtual bool IsReadOnly => dictionary.IsReadOnly;
    
    public virtual void Add(TKey key, TCollection value) {
        ValidateOrThrowValue(ref value);
        dictionary.AutoAdd(key, value);
    }

    public virtual bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    public virtual bool Remove(TKey key) => dictionary.Remove(key);

    public virtual bool TryGetValue(TKey key, out TCollection value) => dictionary.TryGetValue(key, out value);

    #region [ValidateOrThrow]
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void ValidateOrThrowKey(ref TKey key) {
        if (KEY_IS_CLASS && key == null) {
            throw new NullReferenceException($"{nameof(key)} is null");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void ValidateOrThrowValue(ref TCollection value) {
        if (value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void ValidateOrThrowKeyValue(ref TKey outKey, ref TValue value) {
        if (KEY_IS_CLASS && outKey == null) {
            throw new NullReferenceException($"{nameof(outKey)} is null");
        }

        if (VALUE_IS_CLASS && value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void ValidateOrThrowKeyValue(ref KeyValuePair<TKey, TCollection> item) {
        if (KEY_IS_CLASS && item.Key == null) {
            throw new NullReferenceException($"{nameof(TKey)} is null");
        }

        if (item.Value == null) {
            throw new NullReferenceException($"{nameof(TCollection)} is null");
        }
    }
    
    #endregion
}

public abstract class MultiLayerDictionary<TDictionary, TIDictionary, TKey, TIKey, TValue> : MultiLayerDictionary<TDictionary, TIDictionary, TKey, KeyValuePair<TIKey, TValue>>
    where TDictionary : IDictionary<TKey, TIDictionary>, new()
    where TIDictionary : IDictionary<TIKey, TValue>, new() {
    
    protected readonly bool INNER_KEY_IS_CLASS = typeof(TIKey).IsClass;
    
    public MultiLayerDictionary(TDictionary dictionary) : base(dictionary) { }
    public MultiLayerDictionary(params KeyValuePair<TKey, TIDictionary>[] pairs) : base(pairs) { }
    public MultiLayerDictionary(IEnumerable<KeyValuePair<TKey, TIDictionary>> enumerable) : base(enumerable) { }
    
    public virtual TValue this[TKey outKey, TIKey innerKey] {
        get => dictionary[outKey][innerKey];
        set => Add(outKey, innerKey, value);
    }
    
    public virtual void Add(TKey key, TIKey innerKey, TValue value) {
        ValidateOrThrowKeyValue(ref key, ref innerKey, ref value);
        if (dictionary.TryGetValue(key, out var innerDic) == false) {
            dictionary.Add(key, innerDic = new TIDictionary());
        }
        
        innerDic.Add(innerKey, value);
    }
    
    public bool ContainsKey(TKey key, TIKey innerKey) => dictionary.TryGetValue(key, out var innerDic) && innerDic.ContainsKey(innerKey);

    public virtual void Remove(TKey key, TIKey innerKey) {
        if (dictionary.TryGetValue(key, out var innerDic)) {
            innerDic.Remove(innerKey);
        }
    }

    public bool TryGetValue(TKey key, TIKey innerKey, out TValue value) {
        if (dictionary.TryGetValue(key, out var innerDic) && innerDic.TryGetValue(innerKey, out value)) {
            return true;
        }

        value = default;
        return false;
    }


    #region [ValidateOrThrow]
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ValidateOrThrowKeyValue(ref TKey outKey, ref TIKey innerKey, ref TValue value) {
        if (KEY_IS_CLASS && outKey == null) {
            throw new NullReferenceException($"{nameof(outKey)} is null");
        }

        if (INNER_KEY_IS_CLASS && innerKey == null) {
            throw new NullReferenceException($"{nameof(innerKey)} is null");
        }

        if (VALUE_IS_CLASS && value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }
    }
    
    #endregion
}

[TestRequired]
public class MultiLayerDictionary<TKey, TIKey, TValue> : MultiLayerDictionary<Dictionary<TKey, Dictionary<TIKey, TValue>>, Dictionary<TIKey, TValue>, TKey, TIKey, TValue> {

    public MultiLayerDictionary(Dictionary<TKey, Dictionary<TIKey, TValue>> dictionary) : base(dictionary) { }
    public MultiLayerDictionary(params KeyValuePair<TKey, Dictionary<TIKey, TValue>>[] pairs) : base(pairs) { }
    public MultiLayerDictionary(IEnumerable<KeyValuePair<TKey, Dictionary<TIKey, TValue>>> enumerable) : base(enumerable) { }
}

[TestRequired]
public class MultiLayerConcurrentDictionary<TKey, TIKey, TValue> : MultiLayerDictionary<ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>>, ConcurrentDictionary<TIKey, TValue>, TKey, TIKey, TValue> {
    
    public MultiLayerConcurrentDictionary(Dictionary<TKey, Dictionary<TIKey, TValue>> dictionary) {
        foreach (var (key, value) in dictionary) {
            this.dictionary.TryAdd(key, new ConcurrentDictionary<TIKey, TValue>(value));
        }
    }
    
    public MultiLayerConcurrentDictionary(ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>> dictionary) : base(dictionary) { }
    public MultiLayerConcurrentDictionary(params KeyValuePair<TKey, ConcurrentDictionary<TIKey, TValue>>[] pairs) : base(pairs) { }
    public MultiLayerConcurrentDictionary(IEnumerable<KeyValuePair<TKey, ConcurrentDictionary<TIKey, TValue>>> enumerable) : base(enumerable) { }
}

public abstract class ListDictionary<TDictionary, TList, TKey, TValue> : MultiLayerDictionary<TDictionary, TList, TKey, TValue> where TDictionary : IDictionary<TKey, TList>, new() where TList : IList<TValue>, ICollection<TValue>, new() {
    
    protected ListDictionary() { }
    protected ListDictionary(TDictionary dictionary) : base(dictionary) { }
    protected ListDictionary(params KeyValuePair<TKey, TList>[] pairs) : base(pairs) { }
    protected ListDictionary(IEnumerable<KeyValuePair<TKey, TList>> enumerable) : base(enumerable) { }
    
    public virtual void Add(TKey key, TValue value) {
        if (dictionary.TryGetValue(key, out var list) == false) {
            dictionary.Add(key, list = new TList());
        }
        
        list.Add(value);
    }

    public virtual bool TryGetValue(TKey key, int index, out TValue value) {
        if (dictionary.TryGetValue(key, out var list) && list.IsValidIndex(index)) {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }

    public virtual bool TryGetValue(out TValue value, TKey key, Predicate<TValue> predicate) {
        predicate.ThrowIfNull(nameof(predicate));
        if (dictionary.TryGetValue(key, out var list) && list.TryFirst(out value, predicate)) {
            return true;
        }

        value = default;
        return false;
    }
}

[TestRequired]
public class ListDictionary<TKey, TValue> : ListDictionary<Dictionary<TKey, List<TValue>>, List<TValue>, TKey, TValue> {
    
    public ListDictionary() { }
    public ListDictionary(Dictionary<TKey, List<TValue>> dictionary) : base(dictionary) { }
    public ListDictionary(params KeyValuePair<TKey, List<TValue>>[] pairs) : base(pairs) { }
    public ListDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> enumerable) : base(enumerable) { }

    public TValue this[TKey key, int index] {
        get {
            if (dictionary.TryGetValue(key, out var list) == false) {
                throw new KeyNotFoundException($"{nameof(key)} is invalid || {key}");
            }

            list.ThrowIfInvalidKey(index);
            return list[index];
        }

        set {
            var list = dictionary.GetOrAdd(key);
            list.ThrowIfInvalidKey(index);
            list[index] = value;
        }
    }
}
