using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionManager 
{
    private static IEnumerable<Type> _cachedAllTypes;
    private static IEnumerable<Type> CachedAllTypes() => _cachedAllTypes ??= AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes());
    
    
    private static IEnumerable<Type> _cachedClasses;
    public static IEnumerable<Type> GetTypes() => _cachedClasses ??= CacheClassTypes();
    private static IEnumerable<Type> CacheClassTypes() => CachedAllTypes().Where(type => type.IsClass && type.IsAbstract == false);

    #region [Class]
    
    /// <summary>
    /// T 를 SubClass(상속 관계) 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetSubClassTypes<T>() where T : class => GetTypes().Where(type => type.IsSubclassOf(typeof(T)));
    /// <summary>
    /// T 를 Interface 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetInterfaceTypes<T>() => GetTypes().Where(type => typeof(T).IsAssignableFrom(type));
    /// <summary>
    /// T 를 Attribute 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetAttributeTypes<T>() where T : Attribute => GetTypes().Where(type => type.IsDefined(typeof(T), false));
    
    #endregion

    #region [Attribute]

    /// <summary>
    /// T 를 Attribute 로 가지고 있는 Class Type 의 T Attribute
    /// </summary>
    public static IEnumerable<T> GetAttribute<T>() where T : Attribute => GetAttributeTypes<T>().Select(type => type.GetCustomAttributes(typeof(T), false).FirstOrDefault()).Cast<T>();
    /// <summary>
    /// T 를 Attribute 로 가지고 있는 Class Type 의 T Attribute List
    /// </summary>
    public static IEnumerable<T[]> GetAllAttributes<T>() where T : Attribute => GetAttributeTypes<T>().Select(type => type.GetCustomAttributes(typeof(T), false)).Cast<T[]>();

    #endregion

    #region [Enum]
    
    private static IEnumerable<Type> _cachedEnums;
    public static IEnumerable<Type> GetEnumTypes() => _cachedEnums ??= CachedAllTypes().Where(type => type.IsEnum);
    public static IEnumerable<Type> GetAttributeEnumTypes<T>() where T : Attribute => GetEnumTypes().Where(type => type.IsDefined(typeof(T), false));

    public static bool TryGetAttributeEnumTypes<T>(out IEnumerable<Type> type) where T : Attribute {
        type = GetAttributeEnumTypes<T>();
        return type?.Any() ?? false;
    }

    public static IEnumerable<(T attribute, Type enumType)> GetAttributeEnumInfos<T>() where T : Attribute {
        return GetEnumTypes().Where(type => type.IsDefined(typeof(T), false)).Select(type => (type.GetCustomAttribute<T>(false), type));
    }

    public static bool TryGetAttributeEnumInfos<T>(out IEnumerable<(T attribute, Type enumType)> infos) where T : Attribute {
        infos = GetAttributeEnumInfos<T>();
        return infos != null && infos.Any();
    }

    #endregion

    #region [Extension]

    public static bool ContainsCustomAttribute<T>(this MemberInfo info) where T : Attribute => info.GetCustomAttributes<T>().Any();
    public static bool ContainsCustomAttribute<T>(this Type type) where T : Attribute => type.GetCustomAttributes<T>().Any();

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

    #endregion
}
