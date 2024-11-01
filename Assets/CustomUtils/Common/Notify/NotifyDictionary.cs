using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public abstract class NotifyDictionary<TDictionary, TKey, TValue> : NotifyCollection<TDictionary, KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IXmlSerializable where TDictionary : IDictionary<TKey, TValue>, new() {

    public ICollection<TKey> Keys => collection.Keys;
    public ICollection<TValue> Values => collection.Values;

    protected NotifyDictionary(TDictionary dictionary) : base(dictionary) { }
    protected NotifyDictionary() : this(new TDictionary()) { }

    public virtual void Add(TKey key, TValue value) {
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

    public virtual TValue this[TKey key] {
        get => collection[key];
        set {
            if (collection.ContainsKey(key)) {
                collection[key] = value;
                OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
            } else {
                collection.Add(key, value);
                OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
            }
        }
    }

    #region [XML]
    
    private const string ITEM = "item";
    private const string KEY = "key";
    private const string VALUE = "value";
    
    public XmlSchema GetSchema() => null;

    public void ReadXml(XmlReader reader) {
        if (reader.IsEmptyElement) {
            return;
        }
        
        var keySerializer = new XmlSerializer(typeof(TKey));
        var valueSerializer = new XmlSerializer(typeof(TValue));
        reader.Read();
        
        while (reader.NodeType != XmlNodeType.EndElement) {
            reader.ReadStartElement(ITEM);
            
            reader.ReadStartElement(KEY);
            var key = (TKey) keySerializer.Deserialize(reader);
            reader.ReadEndElement();
            
            reader.ReadStartElement(VALUE);
            var value = (TValue) valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            if (key != null && value != null) {
                collection.TryAdd(key, value);
            }
            
            reader.ReadEndElement();
            reader.MoveToContent();
        }
        
        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer) {
        var keySerializer = new XmlSerializer(typeof(TKey));
        var valueSerializer = new XmlSerializer(typeof(TValue));
        foreach (var (key, value) in collection) {
            writer.WriteStartElement(ITEM);
            
            writer.WriteStartElement(KEY);
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();
            
            writer.WriteStartElement(VALUE);
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();
            
            writer.WriteEndElement();
        }
    }
    
    #endregion
}

public class NotifyDictionary<TKey, TValue> : NotifyDictionary<Dictionary<TKey, TValue>, TKey, TValue> {

    public NotifyDictionary() { }
    public NotifyDictionary(int capacity, IEqualityComparer<TKey> comparer = null) : base(new Dictionary<TKey, TValue>(capacity, comparer ?? EqualityComparer<TKey>.Default)) { }
    public NotifyDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }
    public NotifyDictionary(params KeyValuePair<TKey, TValue>[] pairs) : this(null, pairs) { }

    public NotifyDictionary(IEqualityComparer<TKey> comparer, params KeyValuePair<TKey, TValue>[] pairs) : this(pairs.Length, comparer) {
        foreach (var (key, value) in pairs) {
            collection.TryAdd(key, value);
        }
    }
    
    public NotifyDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null) : this(dictionary.Count, comparer) {
        foreach (var (key, value) in dictionary) {
            collection.TryAdd(key, value);
        }
    }

    public NotifyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable, IEqualityComparer<TKey> comparer = null) : this(enumerable is ICollection<KeyValuePair<TKey, TValue>> castCollection ? castCollection.Count : 0, comparer) {
        foreach (var (key, value) in enumerable) {
            collection.TryAdd(key, value);
        }
    }
}

public class NotifySortedDictionary<TKey, TValue> : NotifyDictionary<SortedDictionary<TKey, TValue>, TKey, TValue> {

    public NotifySortedDictionary() { }
    public NotifySortedDictionary(IComparer<TKey> comparer) : base(new SortedDictionary<TKey, TValue>(comparer ?? Comparer<TKey>.Default)) { }

    public NotifySortedDictionary(IComparer<TKey> comparer, params KeyValuePair<TKey, TValue>[] pairs) : this(comparer) {
        foreach (var (key, value) in pairs) {
            collection.TryAdd(key, value);
        }
    } 
    
    public NotifySortedDictionary(params KeyValuePair<TKey, TValue>[] pairs) : this(null, pairs) { }

    public NotifySortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer = null) : this(comparer) {
        foreach (var (key, value) in dictionary) {
            collection.TryAdd(key, value);
        }
    }
}

public class NotifyConcurrentDictionary <TKey, TValue> : NotifyDictionary<ConcurrentDictionary<TKey, TValue>, TKey, TValue> {

    public NotifyConcurrentDictionary() { }
    public NotifyConcurrentDictionary(IEqualityComparer<TKey> comparer = null) : base(new ConcurrentDictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default)) { }

    public NotifyConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable, IEqualityComparer<TKey> comparer = null) : this(comparer) {
        foreach (var (key, value) in enumerable) {
            collection.TryAdd(key, value);
        }
    }
}

public class NotifyRecordDictionary<TKey, TValue> : NotifyDictionary<TKey, TValue> {
    
    public NotifyRecordDictionary() { }
    public NotifyRecordDictionary(params KeyValuePair<TKey, TValue>[] pairs) : base(pairs) { }
    public NotifyRecordDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
    public NotifyRecordDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable) : base(enumerable) { }
    
    public static bool operator ==(NotifyRecordDictionary<TKey, TValue> x, NotifyRecordDictionary<TKey, TValue> y) {
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        if (x.Count != y.Count) {
            return false;
        }
        
        foreach (var (key, xValue) in x) {
            if (y.TryGetValue(key, out var yValue) == false || xValue.Equals(yValue) == false) {
                return false;
            }
        }
        
        return true;
    }

    public static bool operator !=(NotifyRecordDictionary<TKey, TValue> x, NotifyRecordDictionary<TKey, TValue> y) => (x == y) == false;

    public override bool Equals(object obj) => obj is NotifyRecordDictionary<TKey, TValue> castDictionary && this == castDictionary;
    public override int GetHashCode() => collection.GetHashCode();
}