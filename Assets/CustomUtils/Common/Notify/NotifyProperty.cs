using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using CsvHelper.Configuration;

[Serializable, DataContract]
public class NotifyProperty<TValue> : NotifyField, IEqualityComparer<NotifyProperty<TValue>> {
    
    private static readonly IEqualityComparer<TValue> OnEqualityComparer = EqualityComparer<TValue>.Default;

    private TValue _value;
    
    [DataMember]
    public TValue Value {
        get => _value;
        set {
            if (OnEqualityComparer.Equals(_value, value) == false) {
                _value = value;
                OnChanged.handler?.Invoke(NotifyFieldChangedEventArgs.Empty);
            }
        }
    }

    public NotifyProperty() { }
    public NotifyProperty(TValue value) => _value = value;

    public static bool operator ==(NotifyProperty<TValue> x, NotifyProperty<TValue> y) {
        if (ReferenceEquals(x, y)) {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        return OnEqualityComparer.Equals(x.Value, y.Value);
    }

    public static bool operator !=(NotifyProperty<TValue> x, NotifyProperty<TValue> y) {
        if (ReferenceEquals(x, y)) {
            return false;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return true;
        }

        return OnEqualityComparer.Equals(x.Value, y.Value) == false;
    }

    public static implicit operator TValue(NotifyProperty<TValue> property) => property.Value;
    public override string ToString() => Value != null ? Value.ToString() : base.ToString();

    public bool Equals(NotifyProperty<TValue> x, NotifyProperty<TValue> y) => x != null && y != null && OnEqualityComparer.Equals(x.Value, y.Value);
    public int GetHashCode(NotifyProperty<TValue> obj) => obj.Value.GetHashCode();

    public override bool Equals(object obj) => obj is NotifyProperty<TValue> property && Equals(this, property);
    public override int GetHashCode() => GetHashCode(this);
}

#region [CSV ClassMap]

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
