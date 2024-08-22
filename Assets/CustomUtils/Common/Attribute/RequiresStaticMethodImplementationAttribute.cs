using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class RequiresStaticMethodImplementationAttribute : Attribute {

    public string methodName;
    public Type includeAttributeType;

    public RequiresStaticMethodImplementationAttribute(string methodName) => this.methodName = methodName;
    public RequiresStaticMethodImplementationAttribute(string methodName, Type includeAttributeType) : this(methodName) => this.includeAttributeType = includeAttributeType;
}