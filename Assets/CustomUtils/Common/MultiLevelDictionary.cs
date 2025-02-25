using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public abstract class MultiLevelDictionary<TDictionary, TIDictionary, TKey, TIKey, TValue> : IDictionary<TKey, TIDictionary> 
    where TDictionary : IDictionary<TKey, TIDictionary>, new()
    where TIDictionary : IDictionary<TIKey, TValue>, new() {

    protected TDictionary dictionary;
    
    private readonly bool KEY_IS_CLASS = typeof(TKey).IsClass;
    private readonly bool INNER_KEY_IS_CLASS = typeof(TIKey).IsClass;
    private readonly bool VALUE_IS_CLASS = typeof(TValue).IsClass;

    protected MultiLevelDictionary() => dictionary = new TDictionary();

    public MultiLevelDictionary(TDictionary dictionary) : this() {
        foreach (var (key, value) in dictionary) {
            this.dictionary.Add(key, value);
        }
    }

    public MultiLevelDictionary(params KeyValuePair<TKey, TIDictionary>[] pairs) : this() {
        foreach (var (key, value) in pairs) {
            dictionary.Add(key, value);
        }
    }

    public MultiLevelDictionary(IEnumerable<KeyValuePair<TKey, TIDictionary>> enumerable) : this() {
        foreach (var (key, value) in enumerable) {
            dictionary.Add(key, value);
        }
    }
    
    public virtual TIDictionary this[TKey key] {
        get => dictionary[key];
        set => dictionary[key] = value;
    }

    public virtual TValue this[TKey outKey, TIKey innerKey] {
        get => dictionary[outKey][innerKey];
        set {
            if (dictionary.TryGetValue(outKey, out var innerDic)) {
                innerDic[innerKey] = value;
            }
        }
    }
    
    public virtual ICollection<TKey> Keys => dictionary.Keys;
    public virtual ICollection<TIDictionary> Values => dictionary.Values;
    
    public IEnumerator<KeyValuePair<TKey, TIDictionary>> GetEnumerator() => dictionary.GetEnumerator();

    public virtual IEnumerator<KeyValuePair<TIKey, TValue>> GetEnumerator(TKey key) {
        ValidateOrThrowKey(ref key);
        if (dictionary.TryGetValue(key, out var innerDic)) {
            foreach (var pair in innerDic) {
                yield return pair;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<KeyValuePair<TKey, TIDictionary>>.Add(KeyValuePair<TKey, TIDictionary> item) {
        ValidateOrThrowKeyValue(ref item);
        dictionary.AutoAdd(item.Key, item.Value);
    }

    public virtual void Clear() => dictionary.Clear();
    
    public virtual void Clear(TKey key) {
        ValidateOrThrowKey(ref key);
        if (dictionary.TryGetValue(key, out var innerDic)) {
            innerDic.Clear();
        }
    }

    bool ICollection<KeyValuePair<TKey, TIDictionary>>.Contains(KeyValuePair<TKey, TIDictionary> item) {
        ValidateOrThrowKeyValue(ref item);
        return dictionary.TryGetValue(item.Key, out var innerDic) && innerDic.Equals(item.Value);
    }

    void ICollection<KeyValuePair<TKey, TIDictionary>>.CopyTo(KeyValuePair<TKey, TIDictionary>[] array, int arrayIndex) => dictionary.ToList().CopyTo(array, arrayIndex);
    
    bool ICollection<KeyValuePair<TKey, TIDictionary>>.Remove(KeyValuePair<TKey, TIDictionary> item) {
        ValidateOrThrowKeyValue(ref item);
        return dictionary.Remove(item.Key);
    }

    public virtual int Count => dictionary.Count;
    public virtual bool IsReadOnly => dictionary.IsReadOnly;
    
    public virtual void Add(TKey key, TIDictionary value) {
        ValidateOrThrowValue(ref value);
        dictionary.AutoAdd(key, value);
    }

    public virtual void Add(TKey outKey, TIKey innerKey, TValue value) {
        ValidateOrThrowKeyValue(ref outKey, ref innerKey, ref value);
        if (dictionary.TryGetValue(outKey, out var innerDic) == false) {
             dictionary.Add(outKey, innerDic = new TIDictionary());
        }
        
        innerDic.Add(innerKey, value);
    }

    public virtual bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    public virtual bool ContainsKey(TKey outKey, TIKey innerKey) => dictionary.TryGetValue(outKey, out var innerDic) && innerDic.ContainsKey(innerKey);

    public virtual bool Remove(TKey key) => dictionary.Remove(key);
    public virtual bool Remove(TKey outKey, TIKey innerKey) => dictionary.TryGetValue(outKey, out var innerDic) && innerDic.Remove(innerKey);

    public virtual bool TryGetValue(TKey key, out TIDictionary value) => dictionary.TryGetValue(key, out value);

    public virtual bool TryGetValue(TKey outKey, TIKey innerKey, out TValue value) {
        if (dictionary.TryGetValue(outKey, out var innerDic)) {
            return innerDic.TryGetValue(innerKey, out value);
        }

        value = default;
        return false;
    }

    #region [ValidateOrThrow]
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ValidateOrThrowKey(ref TKey key) {
        if (KEY_IS_CLASS && key == null) {
            throw new NullReferenceException($"{nameof(key)} is null");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ValidateOrThrowValue(ref TIDictionary value) {
        if (value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }
    }
    
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ValidateOrThrowKeyValue(ref KeyValuePair<TKey, TIDictionary> item) {
        if (KEY_IS_CLASS && item.Key == null) {
            throw new NullReferenceException($"{nameof(TKey)} is null");
        }

        if (item.Value == null) {
            throw new NullReferenceException($"{nameof(TIDictionary)} is null");
        }
    }
    
    #endregion
}

public class MultiLevelDictionary<TKey, TIKey, TValue> : MultiLevelDictionary<Dictionary<TKey, Dictionary<TIKey, TValue>>, Dictionary<TIKey, TValue>, TKey, TIKey, TValue> {
    
    public MultiLevelDictionary(Dictionary<TKey, Dictionary<TIKey, TValue>> dictionary) : base(dictionary) { }
    public MultiLevelDictionary(params KeyValuePair<TKey, Dictionary<TIKey, TValue>>[] pairs) : base(pairs) { }
    public MultiLevelDictionary(IEnumerable<KeyValuePair<TKey, Dictionary<TIKey, TValue>>> enumerable) : base(enumerable) {    }
}

public class ConcurrentMultiLevelDictionary<TKey, TIKey, TValue> : MultiLevelDictionary<ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>>, ConcurrentDictionary<TIKey, TValue>, TKey, TIKey, TValue> {

    public ConcurrentMultiLevelDictionary(Dictionary<TKey, Dictionary<TIKey, TValue>> dictionary) {
        foreach (var (key, value) in dictionary) {
            this.dictionary.TryAdd(key, new ConcurrentDictionary<TIKey, TValue>(value));
        }
    }
    
    public ConcurrentMultiLevelDictionary(ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>> dictionary) : base(dictionary) { }
    public ConcurrentMultiLevelDictionary(params KeyValuePair<TKey, ConcurrentDictionary<TIKey, TValue>>[] pairs) : base(pairs) { }
    public ConcurrentMultiLevelDictionary(IEnumerable<KeyValuePair<TKey, ConcurrentDictionary<TIKey, TValue>>> enumerable) : base(enumerable) { }

    public override void Add(TKey key, ConcurrentDictionary<TIKey, TValue> value) { 
        ValidateOrThrowValue(ref value);
        dictionary.AddOrUpdate(key, value, (_, _) => value);
    }

    public override void Add(TKey outKey, TIKey innerKey, TValue value) {
        ValidateOrThrowKeyValue(ref outKey, ref innerKey, ref value);
        dictionary.GetOrAdd(outKey, _ => new ConcurrentDictionary<TIKey, TValue>()).AddOrUpdate(innerKey, value, (_, _) => value);
    }

    public ConcurrentDictionary<TIKey, TValue> GetOrAdd(TKey key) => dictionary.GetOrAdd(key, _ => new ConcurrentDictionary<TIKey, TValue>());
}

