using System.Collections.Generic;

public abstract class CommandField {
    
    public abstract void Pase(string text);
    public abstract bool IsValid();
}

public abstract class CommandCollection<TCollection, TValue> : CommandField where TCollection : ICollection<TValue> {

    protected TCollection collection;
    public TCollection Collection => collection;
}

public abstract class CommandProperty<T> : CommandField {

    protected T value;
    public T Value => value;
}

// public class StringProperty : CommandProperty<string> {
//     
//     public StringProperty() { }
//     public StringProperty(string value) => this.value = value;
//     public override bool IsValid() => string.IsNullOrEmpty(value);
// }