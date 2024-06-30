using System;

public class NameAliasAttribute : Attribute {

    public string nameAlias;

    public NameAliasAttribute(string nameAlias) => this.nameAlias = nameAlias;
}