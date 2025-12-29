using System;

public class InvalidTypeException : Exception {

    public InvalidTypeException(string message) : base(message) { }
}

public class InvalidTypeException<TEnum> : InvalidTypeException where TEnum : Enum {

    public InvalidTypeException(string message) : base(message) { }
    public InvalidTypeException(TEnum type) : base($"{type} is invalid {typeof(TEnum).Name} type") { }
}
