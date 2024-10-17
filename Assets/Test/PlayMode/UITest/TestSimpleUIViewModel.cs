using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public record UIViewModel {
    
    public delegate void NotifyPropertyHandler(string propertyName);
    public SafeDelegate<NotifyPropertyHandler> OnNotifyProperty;

    public delegate void NotifyCollectionHandler(string collectionName, NotifyCollectionChangedEventArgs args);
    public SafeDelegate<NotifyCollectionHandler> OnNotifyCollection;

    public delegate void NotifyDictionaryHandler(string dictionaryName, NotifyCollectionChangedEventArgs args);
    public SafeDelegate<NotifyDictionaryHandler> OnNotifyDictionary;
}

public record TestSimpleUIViewModel : UIViewModel {

    private string _title;
    public string Title {
        get => _title;
        set {
            _title = value;
            OnNotifyProperty.handler?.Invoke(nameof(Title));
        }
    }

    private int _count;
    public int Count {
        get => _count;
        set {
            _count = value;
            OnNotifyProperty.handler?.Invoke(nameof(Count));
        }
    }

    public NotifyCollection<int> Collection = new();
    public NotifyDictionary<int, int> Dictionary = new();

    public TestSimpleUIViewModel() {
        if (string.IsNullOrEmpty(Title)) {
            Title = "Test Title";
        }

        _count = 150;
        Collection.CollectionChanged += args => OnNotifyCollection.handler?.Invoke(nameof(Collection), args);
        Dictionary.DictionaryChanged += args => OnNotifyDictionary.handler?.Invoke(nameof(Dictionary), args);
    }

    public TestSimpleUIViewModel(string title) : this() => _title = title;

    public void IncreaseCount(int count) {
        Count += 10;
        Collection.Add(Count);
    }

    public void DecreaseCount(int count) {
        Count -= count;
        Dictionary.TryAdd(Count, Count);
    }
}

public class NotifyCollection<T> : Collection<T>, IDisposable {
    
    public delegate void CollectionChangedHandler(NotifyCollectionChangedEventArgs args);
    public SafeDelegate<CollectionChangedHandler> CollectionChanged;
    
    private bool _isDisposed;

    ~NotifyCollection() {
        if (_isDisposed == false) {
            Dispose();
        }
    }
    
    public void Dispose() {
        CollectionChanged.Clear();
        _isDisposed = true;
    }

    protected override void ClearItems() {
        base.ClearItems();
        CollectionChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void InsertItem(int index, T item) {
        base.InsertItem(index, item);
        CollectionChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    protected override void RemoveItem(int index) {
        base.RemoveItem(index);
        CollectionChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
    }

    protected override void SetItem(int index, T item) {
        base.SetItem(index, item);
        CollectionChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
    }
}

public class NotifyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable {

    public delegate void DictionaryChangedHandler(NotifyCollectionChangedEventArgs args);
    public SafeDelegate<DictionaryChangedHandler> DictionaryChanged;
    
    private bool _isDisposed;

    private Dictionary<TKey, TValue> _dictionary = new();

    public TValue this[TKey key] {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    public ICollection<TKey> Keys => _dictionary.Keys;
    public ICollection<TValue> Values => _dictionary.Values;
    public int Count => _dictionary.Count;
    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>) _dictionary).IsReadOnly;
    
    ~NotifyDictionary() {
        if (_isDisposed == false) {
            Dispose();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() {
        DictionaryChanged.Clear();
        _isDisposed = true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) {
        _dictionary.Add(item.Key, item.Value);
        DictionaryChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public void Clear() {
        _dictionary.Clear();
        DictionaryChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (((ICollection<KeyValuePair<TKey, TValue>>) _dictionary).Remove(item)) {
            DictionaryChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return true;
        }
        
        return false;
    }

    public void Add(TKey key, TValue value) {
        _dictionary.Add(key, value);
        DictionaryChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
    
    public bool Remove(TKey key) {
        if (_dictionary.Remove(key)) {
            DictionaryChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
}