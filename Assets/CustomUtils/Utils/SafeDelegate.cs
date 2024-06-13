using System;
using System.Linq;

public struct SafeDelegate<T> where T : Delegate {
        
    public T handler;
    
    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, T addEvent) {
        if (safeDelegate.handler == null || safeDelegate.handler.GetInvocationList().Contains(addEvent) == false) {
            safeDelegate.handler = Delegate.Combine(safeDelegate.handler, addEvent) as T;
        }
        
        return safeDelegate;
    }
    
    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, Delegate addEvent) {
        if (addEvent is T) {
            if (safeDelegate.handler == null || safeDelegate.handler.GetInvocationList().Contains(addEvent) == false) {
                safeDelegate.handler = Delegate.Combine(safeDelegate.handler, addEvent) as T;
            }
        } else {
            Logger.TraceError($"Invalid delegate Type || {nameof(safeDelegate)} Type = {typeof(T).Name} || {nameof(addEvent)} Type = {addEvent.GetType().Name}");
        }
        
        return safeDelegate;
    }

    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, Delegate[] addEvents) {
        if (addEvents is { Length: > 0 }) {
            foreach (var addEvent in addEvents) {
                safeDelegate += addEvent;
            }
        }
        
        return safeDelegate;
    }

    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, SafeDelegate<T> addSafeDelegate) {
        safeDelegate += addSafeDelegate.GetInvocationList();
        return safeDelegate;
    }

    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, T removeEvent) {
        if (safeDelegate.handler == null) {
            return safeDelegate;
        }

        safeDelegate.handler = Delegate.Remove(safeDelegate.handler, removeEvent) as T;
        return safeDelegate;
    }

    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, Delegate removeEvent) {
        if (removeEvent is T) {
            if (safeDelegate.handler != null) {
                safeDelegate.handler = Delegate.Remove(safeDelegate.handler, removeEvent) as T;
            }
        } else {
            Logger.TraceError($"Invalid delegate Type || {nameof(safeDelegate)} Type = {typeof(T).Name} || {nameof(removeEvent)} Type = {removeEvent.GetType().Name}");
        }

        return safeDelegate;
    }

    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, Delegate[] removeEvents) {
        if (removeEvents is { Length: > 0 }) {
            foreach (var removeEvent in removeEvents) {
                safeDelegate -= removeEvent;
            }
        }

        return safeDelegate;
    }

    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, SafeDelegate<T> removeSafeDelegate) {
        safeDelegate -= removeSafeDelegate.GetInvocationList();
        return safeDelegate;
    }

    public Delegate[] GetInvocationList() => handler?.GetInvocationList();

    public void Clear() {
        if (handler != null) {
            Delegate.RemoveAll(handler, handler);
        }
    }
}
