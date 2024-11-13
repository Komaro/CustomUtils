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
        if (_list.Count <= 0) {
            item = default;
            return false;
        }

        return (item = Pop()) != null;
    }

    public T Pop() {
        if (_list.Count <= 0) {
            return default;
        }
        
        var item = _list[^1];
        _list.RemoveAt(_list.Count - 1);
        _hashSet.Remove(item);
        return item;
    }

    public bool TryPeek(out T item) {
        if (_list.Count <= 0) {
            item = default;
            return false;
        }
        
        return (item = Peek()) != null;
    }

    public T Peek() {
        if (_list.Count <= 0) {
            return default;
        }
    
        return _list[^1];
    }

    public bool TryPeek(int offset, out T item) {
        if (_list.Count <= 0) {
            item = default;
            return false;
        }
        
        return (item = Peek(offset)) != null;
    }

    public T Peek(int offset) {
        if (offset > _list.Count) {
            return default;
        }
        
        return _list[^(offset + 1)];
    }

    public bool TryPeekTail(int offset, out T item) {
        if (_list.Count <= 0) {
            item = default;
            return false;
        }
        
        return (item = PeekTail(offset)) != null;
    }

    public T PeekTail(int offset) {
        if (offset > _list.Count) {
            return default;
        }

        return _list[offset];
    }

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}