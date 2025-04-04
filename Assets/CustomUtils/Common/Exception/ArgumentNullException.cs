using System;

public class ArgumentNullException<T> : ArgumentNullException {

    public ArgumentNullException(string sourceName) : base($"{sourceName}({typeof(T).GetCleanFullName()}) is null") { }
}