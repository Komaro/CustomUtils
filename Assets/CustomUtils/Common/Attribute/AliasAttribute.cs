using System;

public class AliasAttribute : Attribute {

    public readonly string alias;
    
    public AliasAttribute(string alias) => this.alias = alias;
}