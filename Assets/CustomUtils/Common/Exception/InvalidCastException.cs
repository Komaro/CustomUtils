using System;
using System.Reflection;

public class InvalidCastException<T> : InvalidCastException {

    public InvalidCastException(string sourceName) : base($"{sourceName} cannot be cast to {typeof(T).Name}") { }
    public InvalidCastException(MemberInfo sourceType) : base($"{sourceType.Name} cannot be cast to {typeof(T).Name}") { }
}

public class InvalidEnumCastException : InvalidCastException {
    
    private const string FORMAT = "'{0}' cannot be cast to {1}";
    private const string FORMAT_WITH_EXCEPTION = "'{0}' cannot be cast to {1}\n{2}";
    private const string NULL = "null";

    public InvalidEnumCastException(MemberInfo enumType, string value) : base(string.Format(FORMAT, enumType?.Name ?? NULL, value)) { }
    public InvalidEnumCastException(MemberInfo enumType, string value, Exception innerException) : base(string.Format(FORMAT_WITH_EXCEPTION, enumType?.Name ?? NULL, value, innerException)) { }
    
    public InvalidEnumCastException(MemberInfo enumType, int value) : base(string.Format(FORMAT, enumType?.Name ?? NULL, value)) { }
    public InvalidEnumCastException(MemberInfo enumType, int value, Exception innerException) : base(string.Format(FORMAT_WITH_EXCEPTION, enumType?.Name ?? NULL, value, innerException)){ }
    
    public InvalidEnumCastException(MemberInfo enumType, Enum value) : base(string.Format(FORMAT, enumType?.Name ?? NULL, value)){ }
    public InvalidEnumCastException(MemberInfo enumType, Enum value, Exception innerException) : base(string.Format(FORMAT_WITH_EXCEPTION, enumType?.Name ?? NULL, value, innerException)){ }
}