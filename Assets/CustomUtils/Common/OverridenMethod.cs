using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

public readonly struct OverridenMethod {

    private readonly Type _type;
    private readonly HashSet<string> _overrideSet;
    
    public OverridenMethod(Type type, params string[] methodNames) {
        _type = type;
        _overrideSet = new HashSet<string>();
        var methodSet = methodNames.ToHashSetWithDistinct();
        foreach (var info in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            var name = info.GetAlias();
            if (methodSet.Contains(name) && info.GetBaseDefinition().DeclaringType != info.DeclaringType) {
                _overrideSet.Add(name);
            }
        }
    }
    
    public bool HasOverriden(string methodName, bool throwException = false) => throwException
        ? _overrideSet.Contains(methodName) ? true : throw new MissingMethodException(_type.Name, methodName) 
        : _overrideSet.Contains(methodName);
}

public readonly struct AutoOverridenMethod {

    private readonly ImmutableHashSet<MethodBase> _overridenMethodBaseSet;

    public AutoOverridenMethod(Type type) => _overridenMethodBaseSet = type.GetMethods().Where(info => info.IsVirtual && info.DeclaringType == type).Select(info => info as MethodBase).ToImmutableHashSetWithDistinct();

    public bool HasOverriden(MethodBase methodBase, bool throwException = false) => throwException
        ? _overridenMethodBaseSet.Contains(methodBase) ? true : throw new MissingMethodException(methodBase.ReflectedType?.Name, methodBase.GetCleanFullName())
        : _overridenMethodBaseSet.Contains(methodBase);
}
