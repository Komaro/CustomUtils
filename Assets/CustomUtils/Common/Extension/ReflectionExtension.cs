﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionExtension {
    
    public static bool TryGetField(this Type type, string name, out FieldInfo info) {
        info = type.GetField(name);
        return info != null;
    }

    public static bool TryGetMethod(this Type type, string name, out MethodInfo info, BindingFlags bindingFlags = BindingFlags.Default) {
        info = type.GetMethod(name, bindingFlags);
        return info != null;
    }

    public static bool TryGetProperty(this Type type, string name, out PropertyInfo info, BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty) {
        info = type.GetProperty(name, bindingFlags);
        return info != null;
    }

    public static bool TryGetPropertyValue<T>(this Type type, object target, string name, out T value, BindingFlags bindingFlags = BindingFlags.GetProperty | BindingFlags.SetProperty) where T : class {
        if (type.TryGetProperty(name, out var info, bindingFlags)) {
            value = info.GetValue(target) as T;
            return value != null;
        }

        value = null;
        return false;
    }

    public static bool IsDefined<T>(this MemberInfo info) where T : Attribute => info.IsDefined(typeof(T));
    public static bool IsDefined<T>(this Type type) where T : Attribute => type.IsDefined(typeof(T));

    public static bool TryGetCustomAttributeList(this MemberInfo info, out List<Attribute> attributeList) {
        attributeList = info.GetCustomAttributes().ToList();
        return attributeList is { Count: > 0};
    }

    public static bool TryGetCustomAttribute<T>(this Type type, out T attribute) where T : Attribute {
        attribute = type.GetCustomAttribute<T>();
        return attribute != null;
    }
    
    public static bool TryGetCustomAttribute<T>(this MemberInfo info, out T attribute) where T : Attribute {
        attribute = info.GetCustomAttribute<T>();
        return attribute != null;
    }

    public static bool TryGetCustomAttributeList<T>(this Type type, out List<T> attributeList) where T : Attribute {
        attributeList = type.GetCustomAttributes<T>().ToList();
        return attributeList is { Count: > 0 };
    }

    public static bool TryGetCustomAttributeList<T>(this MemberInfo info, out List<T> attributeList) where T : Attribute {
        attributeList = info.GetCustomAttributes<T>().ToList();
        return attributeList is { Count: > 0 };
    }

    public static bool IsStruct(this Type type) => type.IsValueType && type.IsPrimitive == false;
}