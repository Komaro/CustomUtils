using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

[Serializable, DataContract]
public class NotifyProperty<T> : NotifyField, IEqualityComparer<NotifyProperty<T>> {

    private static readonly IEqualityComparer<T> OnEqualityComparer = EqualityComparer<T>.Default;

    private T _value;
    
    [DataMember]
    public T Value {
        get => _value;
        set {
            if (OnEqualityComparer.Equals(_value, value) == false) {
                _value = value;
                OnChanged.handler?.Invoke(NotifyFieldChangedEventArgs.Empty);
            }
        }
    }

    public static bool operator ==(NotifyProperty<T> x, NotifyProperty<T> y) {
        if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        return OnEqualityComparer.Equals(x.Value, y.Value);
    }

    public static bool operator !=(NotifyProperty<T> x, NotifyProperty<T> y) {
        if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) {
            return false;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return true;
        }

        return OnEqualityComparer.Equals(x.Value, y.Value) == false;
    }

    public static implicit operator T(NotifyProperty<T> property) => property.Value;
    public override string ToString() => Value != null ? Value.ToString() : base.ToString();

    public bool Equals(NotifyProperty<T> x, NotifyProperty<T> y) => x != null && y != null && OnEqualityComparer.Equals(x.Value, y.Value);
    public int GetHashCode(NotifyProperty<T> obj) => obj.Value.GetHashCode();

    public override bool Equals(object obj) => obj is NotifyProperty<T> property && Equals(this, property);
    public override int GetHashCode() => Value.GetHashCode();
}