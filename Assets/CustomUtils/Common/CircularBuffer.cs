using System;
using System.Collections;
using System.Collections.Generic;

public class CircularBuffer<T> : IEnumerable<T>, IDisposable {
    
    private readonly T[] _array;
    
    private int _writeIndex;

    public int Count { get; private set; }

    private readonly CircularBufferEnumerator _enumerator;

    private CircularBuffer() => _enumerator = new CircularBufferEnumerator(this);
    public CircularBuffer(int capacity) : this() => _array = new T[capacity];
    
    public CircularBuffer(T[] array) : this(array.Length) {
        array.CopyTo(_array, 0);
        Count = _array.Length;
    }

    public T this[int index] => Get(index);

    public void Dispose() => _enumerator?.Dispose();

    public void Add(T item) {
        _array[_writeIndex] = item;
        _writeIndex = (_writeIndex + 1) % _array.Length;
        
        if (Count < _array.Length) {
            Count++;
        }
    }

    public void AddRange(IEnumerable<T> enumerable) {
        foreach (var item in enumerable) {
            Add(item);
        }
    }

    public T Get(int index) => Count == 0 ? throw new IndexOutOfRangeException() : _array[GetRealIndex(index)];
    public T GetFirst() => Get(0);
    public T GetLast() => Get(Count - 1);

    public IEnumerable<T> GetRange(int index, int length) {
        while (length > 0) {
            yield return Get(index);
            index++;
            length--;
        }
    }

    public void Clear() {
        _writeIndex = 0;
        Count = 0;
        Array.Clear(_array, 0, _array.Length);
    }
    
    public IEnumerator<T> GetEnumerator() {
        _enumerator.Reset();
        return _enumerator;
    }
    
    private int GetRealIndex(int index) {
        index = _writeIndex - Count + Math.Clamp(index, 0, Count - 1);
        if (index < 0) {
            index += _array.Length;
        }

        return index;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool IsValid() => Count > 0;
    
    public class CircularBufferEnumerator : IEnumerator<T> {

        private readonly CircularBuffer<T> _buffer;
        private int _readIndex = -1;

        public CircularBufferEnumerator(CircularBuffer<T> buffer) => _buffer = buffer;

        public void Dispose() { }

        public bool MoveNext() => ++_readIndex < _buffer.Count;

        public void Reset() => _readIndex = -1;
        
        public T Current => _buffer.Get(_readIndex);
        object IEnumerator.Current => Current;
    }
}