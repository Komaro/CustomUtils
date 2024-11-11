using System;
using System.Xml.Serialization;
using Newtonsoft.Json;

public abstract class NotifyField : IDisposable {

    [NonSerialized, JsonIgnore, XmlIgnore]
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

    public abstract void Refresh();
}