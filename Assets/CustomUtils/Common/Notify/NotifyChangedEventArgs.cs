using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

public delegate void NotifyFieldChangedHandler(NotifyFieldChangedEventArgs args);

public class NotifyFieldChangedEventArgs : EventArgs {

    public new static NotifyFieldChangedEventArgs Empty { get; } = new();
}

public class NotifyFieldChangedEventArgs<TValue> : NotifyFieldChangedEventArgs {

    public readonly TValue value;

    public NotifyFieldChangedEventArgs(TValue value) => this.value = value;
}

public class NotifyCollectionChangedEventArgs : NotifyFieldChangedEventArgs {

    public readonly NotifyCollectionChangedAction action;
    
    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action) => this.action = action;
}

public class NotifyCollectionChangedEventArgs<TValue> : NotifyCollectionChangedEventArgs {

    public readonly TValue value;
    
    public NotifyCollectionChangedEventArgs(TValue value, NotifyCollectionChangedAction action) : base(action) => this.value = value;
}

public class NotifyListChangedEventArgs<TValue> : NotifyCollectionChangedEventArgs<TValue> {

    public readonly int index;
    public readonly TValue oldValue;
    
    public NotifyListChangedEventArgs(int index, TValue value, NotifyCollectionChangedAction action) : base(value, action) => this.index = index;
    public NotifyListChangedEventArgs(int index, TValue newValue, TValue oldValue, NotifyCollectionChangedAction action) : this(index, newValue, action) => this.oldValue = oldValue;
}

public class NotifyDictionaryChangedEventArgs<TKey, TValue> : NotifyCollectionChangedEventArgs<TValue> {

    public readonly TKey key;
    
    public NotifyDictionaryChangedEventArgs(TKey key, TValue value, NotifyCollectionChangedAction action) : base(value, action) => this.key = key;
}