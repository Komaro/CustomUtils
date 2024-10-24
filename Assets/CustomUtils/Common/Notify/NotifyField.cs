using System;

public abstract class NotifyField : IDisposable {

    public SafeDelegate<NotifyFieldChangedHandler> OnChanged;

    protected bool isDisposed;

    ~NotifyField() {
        if (isDisposed == false) {
            Dispose();
        }
    }
    
    public virtual void Dispose() {
        OnChanged.Clear();
        GC.SuppressFinalize(this);
        isDisposed = true;
    }
}