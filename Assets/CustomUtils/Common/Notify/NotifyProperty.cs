using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using CsvHelper.Configuration;

[Serializable, DataContract]
public class NotifyProperty<TValue> : NotifyField, IEqualityComparer<NotifyProperty<TValue>> {
    
    private IEqualityComparer<TValue> _equalityComparer;

    [IgnoreDataMember, XmlIgnore]
    public IEqualityComparer<TValue> EqualityComparer {
        get => _equalityComparer;
        set {
            if (ReferenceEquals(value, _equalityComparer) == false) {
                _equalityComparer = value;
            }
        }
    }

    private TValue _value;
    
    [DataMember]
    public TValue Value {
        get => _value;
        set {
            if (_equalityComparer.Equals(_value, value) == false) {
                _value = value;
                OnChanged.handler?.Invoke(NotifyFieldChangedEventArgs.Empty);
            }
        }
    }

    public NotifyProperty(IEqualityComparer<TValue> equalityComparer) => _equalityComparer = equalityComparer;
    public NotifyProperty(TValue value, IEqualityComparer<TValue> equalityComparer) : this(equalityComparer) => _value = value;
    
    public NotifyProperty(TValue value) : this(value, EqualityComparer<TValue>.Default) { }
    public NotifyProperty() : this(EqualityComparer<TValue>.Default) { }

    public static bool operator ==(NotifyProperty<TValue> x, NotifyProperty<TValue> y) {
        if (ReferenceEquals(x, y)) {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
            return false;
        }

        return x.Value.Equals(y.Value);
    }

    public static bool operator !=(NotifyProperty<TValue> x, NotifyProperty<TValue> y) => (x == y) == false;

    public static implicit operator TValue(NotifyProperty<TValue> property) => property.Value;
    public override string ToString() => Value != null ? Value.ToString() : base.ToString();

    public bool Equals(NotifyProperty<TValue> x, NotifyProperty<TValue> y) => x != null && y != null && _equalityComparer.Equals(x.Value, y.Value);
    public int GetHashCode(NotifyProperty<TValue> obj) => obj.Value.GetHashCode();

    public override bool Equals(object obj) => obj is NotifyProperty<TValue> property && Equals(this, property);
    public override int GetHashCode() => GetHashCode(this);
}

#region [CSV ClassMap]

// TODO. 장기적으로 ClassMap을 통합해서 관리하는 시스템 필요
public sealed class NotifyPropertyClassMap<T> : ClassMap<T> {

    public NotifyPropertyClassMap() {
        foreach (var info in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)) {
            if (LambdaExpressionProvider.TryGetMappingExpression<T>(info.Name, out var expression)) {
                Map(expression).Name(info.Name);
            }
        }
    }
}

#endregion
