using System;

public class NullReferenceException<T> : NullReferenceException {

    public NullReferenceException(string sourceName) : base($"{sourceName}({typeof(T).GetCleanFullName()}) is null") { }
}