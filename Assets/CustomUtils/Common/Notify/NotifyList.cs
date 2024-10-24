using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

public class NotifyList<TValue> : NotifyCollection<List<TValue>, TValue>, IList<TValue>, IReadOnlyList<TValue> {

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