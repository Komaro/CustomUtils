using System;

[AttributeUsage(AttributeTargets.Field)]
public class EnumValueAttribute : Attribute {
    
    public readonly string header;

    public EnumValueAttribute(string header = "") => this.header = header;
}