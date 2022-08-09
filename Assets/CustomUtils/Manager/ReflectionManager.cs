using System;
using System.Collections.Generic;
using System.Linq;

public static class ReflectionManager 
{
    private static IEnumerable<Type> _cachedAllTypes;
    private static IEnumerable<Type> CachedAllTypes() => _cachedAllTypes ?? AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes());
    
    
    private static IEnumerable<Type> _cachedClasses;
    private static IEnumerable<Type> CacheClassTypes() => CachedAllTypes().Where(type => type.IsClass && type.IsAbstract == false);
    public static IEnumerable<Type> GetTypes() => _cachedClasses ??= CacheClassTypes();

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
    private static IEnumerable<Type> CacheEnumType() => AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic).SelectMany(assembly => assembly.GetExportedTypes()).Where(type => type.IsEnum);
    public static IEnumerable<Type> GetEnums() => _cachedEnums ?? CachedAllTypes().Where(type => type.IsEnum);
    public static IEnumerable<Type> GetAttributeEnums<T>() where T : Attribute => GetEnums().Where(type => type.IsDefined(typeof(T), false));
    
    #endregion
}
