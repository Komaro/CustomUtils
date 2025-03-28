using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Class)]
public class UIOptionAttribute : Attribute {
    
    public List<Enum> optionTypes;

    public UIOptionAttribute(params object[] optionTypes) => this.optionTypes = optionTypes.ToList<object, Enum>(x => x as Enum);
}