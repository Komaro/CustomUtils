using System;

public class MethodAliasAttribute : Attribute {

    public string alias;

    public MethodAliasAttribute(string alias) => this.alias = alias;
}