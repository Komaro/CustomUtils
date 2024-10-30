using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using CsvHelper.Configuration;

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

# region CSV ClassMap

public sealed class NotifyPropertyClassMap<T> : ClassMap<T> {

    public NotifyPropertyClassMap() {
        var parameter = Expression.Parameter(typeof(T));
        foreach (var info in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)) {
            var fieldType = info.FieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(NotifyProperty<>)) {
                var notifyFieldProperty = Expression.PropertyOrField(parameter, info.Name);
                var valueProperty = Expression.PropertyOrField(notifyFieldProperty, "Value");
                var access = Expression.MakeMemberAccess(notifyFieldProperty, valueProperty.Member);
                var convert = Expression.Convert(access, typeof(object));
                Map(Expression.Lambda<Func<T, object>>(convert, parameter)).Name(info.Name);
            }
        }
    }
}

#endregion
