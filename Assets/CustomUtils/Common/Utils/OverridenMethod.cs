using System;
using System.Collections.Generic;
using System.Reflection;

public struct OverridenMethod {

    private readonly HashSet<string> _overrideSet;

    public OverridenMethod(Type baseType, params string[] methods) {
        _overrideSet = new HashSet<string>();
        var methodSet = methods.ToHashSetWithDistinct();
        foreach (var info in baseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            var name = info.GetAlias();
            if (methodSet.Contains(name) && info.GetBaseDefinition().DeclaringType != info.DeclaringType) {
                _overrideSet.Add(name);
            }
        }
    }
    
    public bool HasOverriden(Type type) => _overrideSet.Contains(type.Name);
    public bool HasOverriden(string type) => _overrideSet.Contains(type);
}