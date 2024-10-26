using System;
using System.Collections.Specialized;

public delegate void NotifyFieldChangedHandler(NotifyFieldChangedEventArgs args);

public class NotifyFieldChangedEventArgs : EventArgs {

    public new static NotifyFieldChangedEventArgs Empty { get; } = new();
}

public class NotifyCollectionChangedEventArgs : NotifyFieldChangedEventArgs {

    public readonly NotifyCollectionChangedAction action;
    
    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action) => this.action = action;
}
