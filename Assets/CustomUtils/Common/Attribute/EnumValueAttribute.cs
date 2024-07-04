using System;

[AttributeUsage(AttributeTargets.Field)]
public class EnumValueAttribute : Attribute {
    
    public string divideText;

    public EnumValueAttribute(string divideText = "") => this.divideText = divideText;
}