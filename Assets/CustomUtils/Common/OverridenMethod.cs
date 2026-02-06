using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEngine.Pool;

public readonly struct OverridenMethod {

    private readonly Type _type;
    private readonly ImmutableHashSet<string> _overrideSet;
    
    public OverridenMethod(Type type, params string[] methodNames) : this(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, methodNames) { }

    public OverridenMethod(Type type, BindingFlags flags, params string[] methodNames) {
        _type = type;
        using var _ = HashSetPool<string>.Get(out var methodSet);
        foreach (var methodName in methodNames) {
            methodSet.Add(methodName);
        }
        
        _overrideSet = type.GetMethods(flags).Where(info => methodSet.Contains(info.GetAlias()) && info.IsVirtual && info.DeclaringType == type).Select(info => info.GetAlias()).ToImmutableHashSetWithDistinct();
    }
    
    public bool HasOverriden(string methodName, bool throwException = false) => throwException
        ? _overrideSet.Contains(methodName) ? true : throw new MissingMethodException(_type.Name, methodName) 
        : _overrideSet.Contains(methodName);
}

public readonly struct AutoOverridenMethod {

    private readonly Type _baseType;
    private readonly ImmutableHashSet<MethodBase> _overridenMethodBaseSet;
    private readonly ImmutableHashSet<string> _overridenMethodNameSet;

    public AutoOverridenMethod(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) {
        _baseType = type;
        _overridenMethodBaseSet = type.GetMethods(flags).Where(info => info.IsVirtual && info.DeclaringType == type).OfType<MethodBase>().ToImmutableHashSetWithDistinct();
        _overridenMethodNameSet = _overridenMethodBaseSet.Select(methodBase => methodBase.GetAlias()).ToImmutableHashSetWithDistinct();
    }

    public AutoOverridenMethod(Type type, Type ignoreType, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) {
        using var _ = HashSetPool<Type>.Get(out var typeSet);
        foreach (var baseType in type.GetBaseTypes(true)) {
            if (baseType != ignoreType) {
                typeSet.Add(baseType);
            }
        }
        
        _baseType = type;
        _overridenMethodBaseSet = type.GetMethods(flags).Where(info => info.IsVirtual && typeSet.Contains(type.DeclaringType)).OfType<MethodBase>().ToImmutableHashSetWithDistinct();
        _overridenMethodNameSet = _overridenMethodBaseSet.Select(methodBase => methodBase.GetAlias()).ToImmutableHashSetWithDistinct();
    }
    
    public bool HasOverriden(MethodBase methodBase, bool throwException = false) => throwException
        ? _overridenMethodBaseSet.Contains(methodBase) ? true : throw new MissingMethodException(_baseType.GetCleanFullName(), methodBase.GetCleanFullName())
        : _overridenMethodBaseSet.Contains(methodBase);

    public bool HasOverriden(string methodName, bool throwException = false) => throwException
        ? _overridenMethodNameSet.Contains(methodName) ? true : throw new MissingMethodException(_baseType.GetCleanFullName(), methodName)
        : _overridenMethodNameSet.Contains(methodName);
}
