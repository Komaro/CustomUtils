using System;
using System.Linq;
using UnityEngine;

public struct SafeDelegate<T> where T : Delegate {

    public T Handler { get; private set; }
    public int Count => GetInvocationList()?.Length ?? 0;
    
    public void Clear() {
        if (Handler != null) {
            Handler = Delegate.RemoveAll(Handler, Handler) as T;
        }
    }

    public readonly bool Contains(T events) {
        foreach (var checkEvent in events.GetInvocationList()) {
            if (GetInvocationList().Contains(checkEvent)) {
                return true;
            }
        }

        return false;
    }

    public readonly Delegate[] GetInvocationList() => Handler?.GetInvocationList() ?? Array.Empty<Delegate>();

    public static T operator +(T events, SafeDelegate<T> addSafeDelegate) {
        foreach (var addEvent in addSafeDelegate.GetInvocationList()) {
            if (events == null || events.GetInvocationList().Contains(addEvent) == false) {
                events = Delegate.Combine(events, addEvent) as T;
            } else {
                Logger.TraceLog($"Already delegate Type || {nameof(addSafeDelegate)} Type = {typeof(T).Name} || {nameof(addEvent)} Type = {addEvent.GetType().Name}", Color.yellow);
            }
        }
        
        return events;
    }
    
    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, T addEvent) {
        if (safeDelegate.Handler == null || safeDelegate.GetInvocationList().Contains(addEvent) == false) {
            safeDelegate.Handler = Delegate.Combine(safeDelegate.Handler, addEvent) as T;
        } else {
            Logger.TraceLog($"Already delegate Type || {nameof(safeDelegate)} Type = {typeof(T).Name} || {nameof(addEvent)} Type = {addEvent.GetType().Name}", Color.yellow);
        }
        
        return safeDelegate;
    }
    
    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, object obj) {
        switch (obj) {
            case T addEvent:
                safeDelegate += addEvent;
                break;
            case Delegate addDelegate:
                safeDelegate += addDelegate;
                break;
            case Delegate[] addEvents:
                safeDelegate += addEvents;
                break;
            case SafeDelegate<T> addSafeDelegate:
                safeDelegate += addSafeDelegate;
                break;
            default:
                Logger.TraceLog($"The {nameof(obj)} type is invalid. It must be either a {nameof(Delegate)} or {nameof(SafeDelegate<T>)} type", Color.yellow);
                break;
        }

        return safeDelegate;
    }

    public static SafeDelegate<T> operator +(SafeDelegate<T> safeDelegate, Delegate addEvent) {
        if (addEvent is T) {
            if (safeDelegate.Handler == null || safeDelegate.GetInvocationList().Contains(addEvent) == false) {
                safeDelegate.Handler = Delegate.Combine(safeDelegate.Handler, addEvent) as T;
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

    public static T operator -(T events, SafeDelegate<T> removeSafeDelegate) {
        if (events == null || removeSafeDelegate.Count <= 0) {
            return events;
        }

        foreach (var removeEvent in removeSafeDelegate.GetInvocationList()) {
            events = Delegate.Remove(events, removeEvent) as T;
        }
        
        return events;
    }
    
    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, T removeEvent) {
        if (safeDelegate.Handler == null) {
            return safeDelegate;
        }

        safeDelegate.Handler = Delegate.Remove(safeDelegate.Handler, removeEvent) as T;
        return safeDelegate;
    }

    public static SafeDelegate<T> operator -(SafeDelegate<T> safeDelegate, Delegate removeEvent) {
        if (removeEvent is T) {
            if (safeDelegate.Handler != null) {
                safeDelegate.Handler = Delegate.Remove(safeDelegate.Handler, removeEvent) as T;
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
}
