using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

// TODO. 일부 메소드 MemberInfo 통합 작업 필요
public static class ReflectionExtension {

    public static string GetAlias(this MemberInfo info, string defaultAlias = "") => info.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : string.IsNullOrEmpty(defaultAlias) ? info.Name : defaultAlias;

    #region [Info]
    
    public static bool TryGetFieldInfo(this Type type, string name, out FieldInfo info) => (info = type.GetField(name)) != null;
    public static bool TryGetFieldInfo(this Type type, string name, BindingFlags bindingFlags, out FieldInfo info) => (info = type.GetField(name, bindingFlags)) != null;
    
    public static bool TryGetMethodInfo(this Type type, string name, out MethodInfo info) => (info = type.GetMethod(name)) != null;
    public static bool TryGetMethodInfo(this Type type, string name, BindingFlags bindingFlags, out MethodInfo info) => (info = type.GetMethod(name, bindingFlags)) != null;
    
    public static bool TryGetPropertyInfo(this Type type, string name, out PropertyInfo info) => (info = type.GetProperty(name)) != null;
    public static bool TryGetPropertyInfo(this Type type, string name, BindingFlags bindingFlags, out PropertyInfo info) => (info = type.GetProperty(name, bindingFlags)) != null;

    public static bool TryGetGetMethod(this PropertyInfo propertyInfo, out MethodInfo methodInfo) => (methodInfo = propertyInfo.GetGetMethod()) != null;
    public static bool TryGetSetMethod(this PropertyInfo propertyInfo, out MethodInfo methodInfo) => (methodInfo = propertyInfo.GetSetMethod()) != null;
    
    #endregion

    #region [Value]

    public static IEnumerable<(string name, object value)> GetAllDataMemberNameWithValue(this Type type, object obj, BindingFlags bindingFlags = default) => type.GetFields(bindingFlags)
            .Select(info => (info.Name, info.GetValue(obj)))
            .Concat(type.GetProperties(bindingFlags).Where(info => info.GetIndexParameters().Length <= 0 && info.GetGetMethod()?.ReturnType is { IsByRef: false }).Select(info => (info.Name, info.GetValue(obj))));
    
    public static bool TryGetFieldValue(this Type type, out object value, object obj, string name) => (value = type.GetFieldValue(obj, name)) != null;
    public static bool TryGetFieldValue(this Type type, out object value, object obj, string name, BindingFlags bindingFlags) => (value = type.GetFieldValue(obj, name, bindingFlags)) != null;

    public static object GetFieldValueFast(this Type type, object obj, string name) => DynamicMethodProvider.GetFieldValueFunc(type, name)?.Invoke(obj);
    
    public static object GetFieldValue(this Type type, object obj, string name) => type.TryGetFieldInfo(name, out var info) ? info.GetValue(obj) : null;
    public static object GetFieldValue(this Type type, object obj, string name, BindingFlags bindingFlags) => type.TryGetFieldInfo(name, bindingFlags, out var info) ? info.GetValue(obj) : null;

    public static bool TryGetPropertyValue(this Type type, out object value, object obj, string name) => (value = type.GetPropertyValue(obj, name)) != null;
    public static bool TryGetPropertyValue(this Type type, out object value, object obj, string name, BindingFlags bindingFlags) => (value = type.GetPropertyValue(obj, name, bindingFlags)) != null;
    
    public static object GetPropertyValue(this Type type, object obj, string name) => type.TryGetPropertyInfo(name, out var info) ? info.GetValue(obj) : null;
    public static object GetPropertyValue(this Type type, object obj, string name, BindingFlags bindingFlags) => type.TryGetPropertyInfo(name, bindingFlags, out var info) ? info.GetValue(obj) : null;

    #endregion

    #region [Attribute]

    public static bool IsDefinedInEnvironment<TAttribute>(this MemberInfo info) where TAttribute : Attribute => info.IsDefined<TAttribute>() && (Application.isEditor == false || info.IsDefined<OnlyEditorEnvironmentAttribute>() == false);
    public static bool IsDefinedInEnvironment<TAttribute>(this Type type) where TAttribute : Attribute => type.IsDefined<TAttribute>() && (Application.isEditor == false || type.IsDefined<OnlyEditorEnvironmentAttribute>() == false);

    public static bool TryGetCustomAttribute<TAttribute>(this MemberInfo info, out TAttribute attribute, Type attributeType, bool inherit = false) where TAttribute : Attribute => (attribute = info.GetCustomAttribute<TAttribute>(attributeType, inherit)) != null;
    public static bool TryGetCustomAttribute<TAttribute>(this MemberInfo info, out TAttribute attribute, bool inherit = false) where TAttribute : Attribute => (attribute = info.GetCustomAttribute<TAttribute>(inherit)) != null;
    
    private static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo info, bool inherit = false) where TAttribute : Attribute => info.GetCustomAttribute<TAttribute>(typeof(TAttribute), inherit);
    private static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo info, Type attributeType, bool inherit = false) where TAttribute : Attribute => info.IsDefined(attributeType, inherit) ? info.GetCustomAttribute(attributeType, inherit) as TAttribute : null;
    
    public static bool TryGetCustomAttributes<TAttribute>(this MemberInfo info, out IEnumerable<TAttribute> enumerable, bool inherit = false) where TAttribute : Attribute => (enumerable = info.GetCustomAttributes<TAttribute>(inherit))!.Any();
    
    public static bool IsDefined<TAttribute>(this MemberInfo info, bool inherit = false) where TAttribute : Attribute => info.IsDefined(typeof(TAttribute), inherit);
    
    public static bool IsContains<TAttribute>(this MemberInfo info) where TAttribute : Attribute => CustomAttributeData.GetCustomAttributes(info).Any(data => data.AttributeType == typeof(TAttribute));

    #endregion
    
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

    public static IEnumerable<Type> GetAllTypes(this Type type) {
        do {
            yield return type;
            foreach (var interfaceType in type.GetInterfaces()) {
                yield return interfaceType;
            }
        } while ((type = type.BaseType) != null);
    }

    public static IEnumerable<Type> GetBaseTypes(this Type type, bool includeSelf = false) {
        if (includeSelf) {
            yield return type;
        }
        
        while ((type = type.BaseType) != null) {
            yield return type;
        }
    }

    public static IEnumerable<Type> GetInterfaceTypes(this Type type) {
        do {
            foreach (var interfaceType in type.GetInterfaces()) {
                yield return interfaceType;
            }
        } while ((type = type.BaseType) != null);
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

    #region [Clean Full Name]

    private static readonly ConcurrentDictionary<MemberInfo, string> _typeCleanFullNameDic = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCleanFullName(this Type type) => _typeCleanFullNameDic.GetOrAdd(type, _ => {
        using (StringUtil.StringBuilderPool.Get(out var builder)) {
            builder.Append(type.Name);
            if (type.IsGenericType) {
                builder.Append($"<{type.GetGenericArguments().ToStringCollection(argumentType => argumentType.GetCleanFullName(), ", ")}>");
            }
            
            return builder.ToString();
        }
    });

    public static string GetCleanFullName(this MethodBase methodBase) => _typeCleanFullNameDic.GetOrAdd(methodBase, _ => {
        using (StringUtil.StringBuilderPool.Get(out var builder)) {
            builder.Append(methodBase.Name);
            if (methodBase.IsGenericMethod) {
                builder.Append($"<{methodBase.GetGenericArguments().ToStringCollection(info => info.GetCleanFullName())}>");
            }

            if (methodBase.GetParameters().IsEmpty() == false) {
                builder.Append($"({methodBase.GetParameters().ToStringCollection(info => info.GetCleanFullName(), ',')})");
            }

            return builder.ToString();
        }
    });
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCleanFullName(this ParameterInfo info) => GetCleanFullName(info.ParameterType);

    #endregion

    public static bool IsStruct(this Type type) => type.IsValueType && type.IsPrimitive == false;
    public static bool IsDelegate(this Type type) => type.IsSubclassOf(typeof(Delegate));
    
    #region [PriorityExtension]
    
    public static uint GetOrderByPriority(this Type type) => type.TryGetCustomAttribute<PriorityAttribute>(out var attribute, true) ? attribute.priority : 99999;

    #endregion
}