using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionExtension {

    public static string GetAlias(this Type type, string defaultAlias = "") => type.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : string.IsNullOrEmpty(defaultAlias) ? type.Name : defaultAlias;
    public static string GetAlias(this MethodInfo info, string defaultAlias = "") => info.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : string.IsNullOrEmpty(defaultAlias) ? info.Name : defaultAlias;

    public static IEnumerable<(string name, object value)> GetAllDataMemberNameWithValue(this Type type, object ob, BindingFlags bindingFlags = default) => 
        type.GetFields(bindingFlags).ConvertTo(info => (info.Name, info.GetValue(ob)))
        .Concat(type.GetProperties(bindingFlags).Where(info => info.GetIndexParameters().Length <= 0).ConvertTo(info => (info.Name, info.GetValue(ob))));

    public static bool TryGetFieldInfo(this Type type, out FieldInfo info, string name, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) => (info = type.GetField(name, bindingFlags)) != null;
    public static bool TryGetMethodInfo(this Type type, out MethodInfo info, string name, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) => (info = type.GetMethod(name, bindingFlags)) != null;
    public static bool TryGetPropertyInfo(this Type type, out PropertyInfo info, string name, BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty) => (info = type.GetProperty(name, bindingFlags)) != null;

    public static bool TryGetFieldValue<TValue>(this Type type, out TValue value, object obj, string name, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) => (value = type.GetFieldValue<TValue>(obj, name, bindingFlags)) != null;
    public static TValue GetFieldValue<TValue>(this Type type, object obj, string name, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) => type.TryGetFieldInfo(out var fieldInfo, name, bindingFlags) && fieldInfo.GetValue(obj) is TValue value ? value : default;

    public static bool TryGetPropertyValue<T>(this Type type, out T value, object target, string name, BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty) where T : class {
        if (type.TryGetPropertyInfo(out var info, name, bindingFlags)) {
            return (value = info.GetValue(target) as T) != null;
        }

        value = null;
        return false;
    }

    public static bool IsDefined<T>(this MemberInfo info) where T : Attribute => info.IsDefined(typeof(T));
    public static bool IsDefined<T>(this Type type) where T : Attribute => type.IsDefined(typeof(T));

    public static bool TryGetCustomAttribute<TAttribute>(this Type type, out TAttribute attribute) where TAttribute : Attribute => (attribute = type.GetCustomAttribute<TAttribute>()) != null;
    public static bool TryGetCustomAttribute<TAttribute>(this Type type, Type attributeType, out TAttribute attribute) where TAttribute : Attribute => (attribute = type.GetCustomAttribute(attributeType) as TAttribute) != null;
    public static bool TryGetCustomInheritedAttribute<TBaseAttribute>(this Type type, out TBaseAttribute attribute) where TBaseAttribute : Attribute => (attribute = type.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().IsSubclassOf(typeof(TBaseAttribute))) as TBaseAttribute) != null;
    public static bool TryGetCustomAttributeList<TAttribute>(this Type type, out List<TAttribute> attributeList) where TAttribute : Attribute => (attributeList = type.GetCustomAttributes<TAttribute>().ToList()) is { Count: > 0 };

    public static bool TryGetCustomAttributeList<TAttribute>(this MemberInfo info, out List<TAttribute> attributeList) where TAttribute : Attribute => (attributeList = info.GetCustomAttributes<TAttribute>().ToList()) is { Count: > 0 };
    public static bool TryGetCustomAttribute<TAttribute>(this MemberInfo info, out TAttribute attribute) where TAttribute : Attribute => (attribute = info.GetCustomAttribute<TAttribute>()) != null;
    
    public static bool IsGenericCollectionType(this Type type) {
        if (type.IsGenericType == false) {
            return false;
        }

        foreach (var interfaceType in type.GetInterfaces()) {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<Type> GetBaseTypes(this Type type) {
        while ((type = type.BaseType) != null) {
            yield return type;
        }
    }

    public static IEnumerable<Type> GetGenericArguments(this Type type, Type baseDefinitionType) {
        if (baseDefinitionType.IsGenericType == false) {
            Logger.TraceError($"{baseDefinitionType.Name} is not generic {nameof(Type)}");
            return Enumerable.Empty<Type>();
        }
        
        foreach (var baseType in type.GetBaseTypes()) {
            if (baseType.IsGenericType == false) {
                continue;
            }
            
            if (baseType.GetGenericTypeDefinition() == baseDefinitionType) {
                return baseType.GetGenericArguments();
            }
        }
        
        return Enumerable.Empty<Type>();
    }
    

    public static bool IsStruct(this Type type) => type.IsValueType && type.IsPrimitive == false;
    public static bool IsDelegate(this Type type) => type.IsSubclassOf(typeof(Delegate));


    #region PriorityExtension
    
    public static uint GetOrderByPriority(this Type type) {
        if (type.TryGetCustomInheritedAttribute<PriorityAttribute>(out var attribute)) {
            return attribute.priority;
        }

        return 99999;
    }
    
    #endregion
}