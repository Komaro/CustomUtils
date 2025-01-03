using System;
using System.Collections;
using System.Collections.Generic;

public class CallStack<T> : IEnumerable<T> {

    private readonly List<T> _list = new();
    private readonly HashSet<T> _hashSet = new();

    public int Count => _list.Count;
    
    public void Push(T item) {
        if (_hashSet.Contains(item)) {
            _list.RemoveAt(_list.IndexOf(item));
        } else {
            _hashSet.Add(item);
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

    public T Peek(int offset = 0) {
        if (_list.Count <= offset) {
            throw new InvalidOperationException();
        }

        return _list[^(1 + offset)];
    }

    public bool TryPeekTail(out T item, int offset = 0) {
        try {
            item = PeekTail(offset);
            return true;
        } catch (InvalidOperationException) {
            item = default;
            return false;
        }
    }

    public T PeekTail(int offset = 0) {
        if (offset >= _list.Count) {
            throw new InvalidOperationException();
        }

        return _list[offset];
    }
    
    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}