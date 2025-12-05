using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CallStack<T> : IEnumerable<T> {

    private readonly List<T> _list;
    private readonly HashSet<T> _hashSet;
    
    public int Count => _list.Count;

    public CallStack(int capacity) {
        _list = new List<T>(capacity);
        _hashSet = new HashSet<T>(capacity);
    }

    public CallStack() : this(10) { }
    
    public CallStack(IEnumerable<T> collection) : this(10) {
        foreach (var value in collection.Distinct()) {
            _list.Add(value);
            _hashSet.Add(value);
        }
    }

    public void Push(T item) {
        if (_hashSet.Add(item) == false) {
            _list.RemoveAt(_list.IndexOf(item));
        }

        _list.Add(item);
    }
    
    public bool TryPop(out T item) {
        try {
            item = Pop();
            return true;
        } catch (InvalidOperationException) {
            item = default;
            return false;
        }
    }

    public T Pop() {
        if (_list.Count <= 0) {
            throw new InvalidOperationException();
        }
    
        var item = _list[^1];
        _list.RemoveAt(_list.Count - 1);
        _hashSet.Remove(item);
        return item;
    }

    public bool TryPeek(out T item, int offset = 0) {
        try {
            item = Peek(offset);
            return true;
        } catch (InvalidOperationException) {
            item = default;
            return false;
        }
    }

    public T Peek(int offset = 0) => _list.Count <= offset ? throw new InvalidOperationException() : _list[^(1 + offset)];

    public bool TryPeekTail(out T item, int offset = 0) {
        try {
            item = PeekTail(offset);
            return true;
        } catch (InvalidOperationException) {
            item = default;
            return false;
        }
    }

    public T PeekTail(int offset = 0) => offset >= _list.Count ? throw new InvalidOperationException() : _list[offset];

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}