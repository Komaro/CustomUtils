using System.Collections.Generic;

public class NotifyProperty<T> : NotifyField, IEqualityComparer<NotifyProperty<T>> {

    private static readonly IEqualityComparer<T> OnEqualityComparer = EqualityComparer<T>.Default;
    
    private T _value;
    public T Value {
        get => _value;
        set {
            if (OnEqualityComparer.Equals(_value, value) == false) {
                _value = value;
                OnChanged.handler?.Invoke(NotifyFieldChangedEventArgs.Empty);
            }
        }
    }

    public static implicit operator T(NotifyProperty<T> property) => property.Value;
    public override string ToString() => Value != null ? Value.ToString() : base.ToString();
    
    public bool Equals(NotifyProperty<T> x, NotifyProperty<T> y) => x != null && y != null && OnEqualityComparer.Equals(x.Value, y.Value);
    public int GetHashCode(NotifyProperty<T> obj) => obj.Value.GetHashCode();
}