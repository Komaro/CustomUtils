using System;

public class InvalidCastException<TTarget> : InvalidCastException {

    public InvalidCastException(string sourceName) : base($"{sourceName} cannot be cast to {typeof(TTarget).Name}") { }
    public InvalidCastException(Type sourceType) : base($"{sourceType.Name} cannot be cast to {typeof(TTarget).Name}") { }
}