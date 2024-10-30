using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public abstract class NotifyCollection<TCollection, TValue> : NotifyField, ICollection<TValue>, IReadOnlyCollection<TValue> where TCollection : ICollection<TValue>, new() {

    protected readonly TCollection collection = new();
    
    public int Count => collection.Count;
    public bool IsReadOnly => collection.IsReadOnly;

    public override void Dispose() {
        if (typeof(IDisposable).IsAssignableFrom(typeof(TValue))) {
            collection.SafeClear(x => (x as IDisposable)?.Dispose());
        } else {
            collection.Clear();
        }

        base.Dispose();
    }

    public void Add(TValue item) {
        if (item == null) {
            throw new NullReferenceException();
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
            throw new NullReferenceException();
        }
        
        return collection.Contains(item);
    }

    public void CopyTo(TValue[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

    public bool Remove(TValue item) {
        if (item == null) {
            throw new NullReferenceException();
        }

        if (collection.Remove(item)) {
            OnChanged.handler?.Invoke(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return true;
        }

        return false;
    }
    
    public IEnumerator<TValue> GetEnumerator() => collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class NotifyCollection<TValue> : NotifyCollection<Collection<TValue>, TValue>, IList<TValue> {

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