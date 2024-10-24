using System;
using System.Collections.Specialized;

public delegate void NotifyFieldChangedHandler(NotifyFieldChangedEventArgs args);

public class NotifyFieldChangedEventArgs : EventArgs {

    private static readonly NotifyFieldChangedEventArgs _empty = new();
    public static NotifyFieldChangedEventArgs Empty => _empty;
}

public class NotifyCollectionChangedEventArgs : NotifyFieldChangedEventArgs {

    public readonly NotifyCollectionChangedAction action;
    
    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action) => this.action = action;
}
