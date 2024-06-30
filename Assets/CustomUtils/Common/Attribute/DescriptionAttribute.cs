using System;

public class DescriptionAttribute : Attribute {

    public string description;

    public DescriptionAttribute(string description) => this.description = description;
}