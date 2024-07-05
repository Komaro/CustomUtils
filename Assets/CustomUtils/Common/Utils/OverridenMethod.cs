using System;
using System.Collections.Generic;
using System.Reflection;

public struct OverridenMethod {

    private readonly HashSet<string> _overrideSet;

    public OverridenMethod(Type type, params string[] methods) {
        _overrideSet = new HashSet<string>();
        foreach (var method in methods) {
            if (type.TryGetMethod(method, out var info, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                var declaringType = info.GetBaseDefinition().DeclaringType;
                if (declaringType != info.DeclaringType) {
                    _overrideSet.Add(method);
                }
            }
        }
    }
        
    public bool HasOverriden(Type type) => _overrideSet.Contains(type.Name);
    public bool HasOverriden(string type) => _overrideSet.Contains(type);
}