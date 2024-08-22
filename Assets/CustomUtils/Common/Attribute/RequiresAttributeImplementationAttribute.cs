using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class RequiresAttributeImplementationAttribute : Attribute {

    public Type implementTargetAttributeType;

    public RequiresAttributeImplementationAttribute(Type implementTargetAttributeType) => this.implementTargetAttributeType = implementTargetAttributeType;
}