using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class)]
public class UIOptionAttribute : Attribute {
    
    public List<Enum> optionTypes;

    public UIOptionAttribute(params object[] optionTypes) => this.optionTypes = optionTypes.ConvertTo(x => x as Enum);
}