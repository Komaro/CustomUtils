using System;

public class AliasAttribute : Attribute {

    public string alias;
    
    public AliasAttribute(string alias) => this.alias = alias;
}