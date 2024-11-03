using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

[Serializable, DataContract]
public abstract class NotifyCollection<TCollection, TValue> : NotifyField, ICollection, ICollection<TValue>, IReadOnlyCollection<TValue> where TCollection : ICollection<TValue>, new() {

    [DataMember] 
    protected readonly TCollection collection;

    public int Count => collection.Count;
    public bool IsReadOnly => collection.IsReadOnly;
    public bool IsSynchronized => (collection as ICollection)?.IsSynchronized ?? false;
    public object SyncRoot => (collection as ICollection)?.SyncRoot ?? false;

    protected NotifyCollection() => collection = new TCollection();
    protected NotifyCollection(TCollection collection) => this.collection = collection;

    public override void Dispose() {
        if (typeof(IDisposable).IsAssignableFrom(typeof(TValue))) {
            collection?.SafeClear(x => (x as IDisposable)?.Dispose());
        } else {
            collection?.Clear();
        }

        base.Dispose();
    }

    public void Add(TValue item) {
        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }
        
        collection.Add(item);
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public void Clear() {
        collection.Clear();
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(TValue item) {
        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }
        
        return collection.Contains(item);
    }

    public void CopyTo(TValue[] array, int index) => collection.CopyTo(array, index);

    public void CopyTo(Array array, int index) {
        if (array is not TValue[] castArray) {
            throw new InvalidCastException($"The {nameof(array)} cannot be cast to type {nameof(TValue)}");
        }
        
        collection.CopyTo(castArray, index);
    }

    public bool Remove(TValue item) {
        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }

        if (collection.Remove(item)) {
            OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return true;
        }

        return false;
    }
    
    public IEnumerator<TValue> GetEnumerator() => collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region [Operator]
    
    public static bool operator ==(NotifyCollection<TCollection, TValue> x, IList y) {
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        using (var xEnumerator = x.GetEnumerator()) {
            var yEnumerator = y.GetEnumerator();
            while (xEnumerator.MoveNext()) {
                if (yEnumerator.MoveNext() == false || yEnumerator.Current?.Equals(xEnumerator.Current) == false) {
                    return false;
                }
            }
            
            if (yEnumerator.MoveNext()) {
                return false;
            }
        }
        
        return true;
    }

    public static bool operator !=(NotifyCollection<TCollection, TValue> x, IList y) => (x == y) == false;
    
    public static bool operator ==(NotifyCollection<TCollection, TValue> x, IEnumerable<TValue> y) {
        if (ReferenceEquals(x, y)) {
            return true;
        }
        
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        return x.SequenceEqual(y);
    }
    
    public static bool operator !=(NotifyCollection<TCollection, TValue> notifyCollection, IEnumerable<TValue> enumerable) => (notifyCollection == enumerable) == false;

    #endregion
}

public class NotifyCollection<TValue> : NotifyCollection<Collection<TValue>, TValue>, IList<TValue> {

    public NotifyCollection() { }

    public NotifyCollection(IEnumerable<TValue> enumerable) {
        foreach (var value in enumerable) {
            collection.Add(value);
        }
    }

    public NotifyCollection(IList<TValue> list) : this((IEnumerable<TValue>)list) { }
    public NotifyCollection(params TValue[] values) : this((IEnumerable<TValue>)values) { }
    
    public int IndexOf(TValue item) {
        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }
        
        return collection.IndexOf(item);
    }

    public void Insert(int index, TValue item) {
        if (index >= Count || index < 0) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }

        collection.Insert(index, item);
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public void RemoveAt(int index) {
        if (index >= Count) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        collection.RemoveAt(index);
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
    }

    public TValue this[int index] {
        get => collection[index];
        set {
            collection[index] = value;
            OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
        }
    }
}

public class NotifyRecordCollection<TValue> : NotifyCollection<TValue> {

    public NotifyRecordCollection() { }
    public NotifyRecordCollection(params TValue[] values) : base(values) { }
    public NotifyRecordCollection(IEnumerable<TValue> enumerable) : base(enumerable) { }

    public static bool operator ==(NotifyRecordCollection<TValue> x, NotifyRecordCollection<TValue> y) {
        if (ReferenceEquals(x, null)  || ReferenceEquals(y, null)) {
            return false;
        }

        if (x.Count != y.Count) {
            return false;
        }
        
        for (var index = 0; index < x.Count; index++) {
            if (x[index].Equals(y[index]) == false) {
                return false;
            }
        }

        return true;
    }

    public static bool operator !=(NotifyRecordCollection<TValue> x, NotifyRecordCollection<TValue> y) => (x == y) == false;

    public override bool Equals(object obj) => obj is NotifyRecordCollection<TValue> castCollection && this == castCollection;
    public override int GetHashCode() => collection.GetHashCode();
}