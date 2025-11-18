using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using JetBrains.Annotations;

public class NotifyList<TValue> : NotifyCollection<List<TValue>, TValue>, IList<TValue>, IReadOnlyList<TValue> {

    public int Capacity { get => collection.Capacity; set => collection.Capacity = value; }
    
    public NotifyList() { }
    public NotifyList(int capacity) : base(new List<TValue>(capacity)) { }
    public NotifyList(IEnumerable<TValue> values) : base(new List<TValue>(values)) { }
    public NotifyList(params TValue[] values) : base(new List<TValue>(values)) { }

    public int IndexOf(TValue item) {
        if (item == null) {
            throw new NullReferenceException($"${nameof(item)} is null");
        }

        return collection.IndexOf(item);
    }

    public void Insert(int index, TValue item) {
        if (index > Count || index < 0) {
            throw new ArgumentOutOfRangeException(nameof(item));
        }
        
        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }
        
        collection.Insert(index, item);
        OnChanged.Handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public virtual void InsertWithDetails(int index, [CanBeNull]TValue item) {
        if (index > Count || index < 0) {
            throw new ArgumentOutOfRangeException(nameof(item));
        }

        if (item == null) {
            throw new NullReferenceException($"{nameof(item)} is null");
        }
        
        collection.Insert(index, item);
        OnChanged.Handler?.Invoke(new NotifyListChangedEventArgs<TValue>(index, item, NotifyCollectionChangedAction.Add));
    }

    public void RemoveAt(int index) {
        if (index >= Count) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        collection.RemoveAt(index);
        OnChanged.Handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
    }

    public virtual void RemoveAtWithDetails(int index) {
        if (index >= Count) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        collection.RemoveAt(index, out var item);
        OnChanged.Handler?.Invoke(new NotifyListChangedEventArgs<TValue>(index, item, NotifyCollectionChangedAction.Remove));
    }
    
    public TValue this[int index] {
        get => collection[index];
        set => Replace(index, value);
    }

    public void Replace(int index, [CanBeNull]TValue value) {
        if (collection.IsValidIndex(index) == false) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }

        collection[index] = value;
        OnChanged.Handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
    }

    public virtual void ReplaceWithDetails(int index, [CanBeNull]TValue value) {
        if (collection.IsValidIndex(index) == false) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (value == null) {
            throw new NullReferenceException($"{nameof(value)} is null");
        }

        var oldValue = collection[index];
        collection[index] = value;
        OnChanged.Handler?.Invoke(new NotifyListChangedEventArgs<TValue>(index, value, oldValue, NotifyCollectionChangedAction.Replace));
    }

    public override void Refresh() => OnChanged.Handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
}

// record 형식과 동일하게 Reference 대신 데이터를 비교 
public class NotifyRecordList<TValue> : NotifyList<TValue> {

    public NotifyRecordList() { }
    public NotifyRecordList(int capacity) : base(capacity) { }
    public NotifyRecordList(IEnumerable<TValue> values) : base(values) { }
    public NotifyRecordList(params TValue[] values) : base(values) { }
    
    public static bool operator ==(NotifyRecordList<TValue> x, NotifyRecordList<TValue> y) {
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
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

    public static bool operator !=(NotifyRecordList<TValue> x, NotifyRecordList<TValue> y) => (x == y) == false;

    public override bool Equals(object obj) => obj is NotifyRecordList<TValue> castList && this == castList;
    public override int GetHashCode() => collection.GetHashCode();
}