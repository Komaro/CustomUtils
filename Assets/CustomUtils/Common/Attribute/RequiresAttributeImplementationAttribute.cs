using System;

[AttributeUsage(AttributeTargets.Class)]
public class RequiresAttributeImplementationAttribute : Attribute {

    public Type implementTargetAttributeType;

    public RequiresAttributeImplementationAttribute(Type implementTargetAttributeType) => this.implementTargetAttributeType = implementTargetAttributeType;
}