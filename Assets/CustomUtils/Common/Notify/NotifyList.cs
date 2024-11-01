using System;
using System.Collections.Generic;
using System.Collections.Specialized;

public class NotifyList<TValue> : NotifyCollection<List<TValue>, TValue>, IList<TValue>, IReadOnlyList<TValue> {

    public int Capacity { get => collection.Capacity; set => collection.Capacity = value; }
    
    public NotifyList() { }
    public NotifyList(int capacity) : base(new List<TValue>(capacity)) { }
    public NotifyList(IEnumerable<TValue> values) : base(new List<TValue>(values)) { }
    public NotifyList(params TValue[] values) : base(new List<TValue>(values)) { }

    public int IndexOf(TValue item) {
        if (item == null) {
            throw new NullReferenceException();
        }

        return collection.IndexOf(item);
    }

    public void Insert(int index, TValue item) {
        if (item == null) {
            throw new NullReferenceException();
        }
        
        collection.Insert(index, item);
        OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
    }

    public void RemoveAt(int index) {
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