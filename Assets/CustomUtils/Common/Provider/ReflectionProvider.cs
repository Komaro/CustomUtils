using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

public static class ReflectionProvider {
    
    private static class Cache {

        public static readonly ImmutableArray<Type> CachedTypes = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes()).ToImmutableArray();
        public static readonly ImmutableArray<Type> CachedClasses = CachedTypes.Where(type => type.IsClass).ToImmutableArray();
        public static readonly ImmutableArray<Type> CachedEnums = CachedTypes.Where(type => type.IsEnum && type.ContainsGenericParameters == false).ToImmutableArray();
    }
    
    public static IEnumerable<Type> GetTypes() => Cache.CachedTypes;
    public static IEnumerable<Type> GetClassTypes() => Cache.CachedClasses;
    public static IEnumerable<Type> GetEnumTypes() => Cache.CachedEnums;

    #region [Class]

    /// <summary>
    /// T를 상속 관계로 가지는 SubTypes
    /// </summary>
    public static IEnumerable<Type> GetSubTypesOfType<T>() where T : class => GetSubTypesOfType(typeof(T));
    
    /// <summary>
    /// type을 상속 관계로 가지는 SubTypes
    /// </summary>
    public static IEnumerable<Type> GetSubTypesOfType(Type type) => Cache.CachedClasses.Where(subType => subType.IsSubclassOf(type));

    /// <summary>
    /// typeDefinition을 상속 관계로 가지는 SubTypes
    /// </summary>
    public static IEnumerable<Type> GetSubTypesOfTypeDefinition(Type typeDefinition) => Cache.CachedClasses.Where(subType => {
        if (subType.IsAbstract) {
            return false;
        }

        foreach (var type in subType.GetBaseTypes()) {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeDefinition) {
                return true;
            }
        }
        
        return false;
    });

    /// <summary>
    /// T를 Interface로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetInterfaceTypes<T>() => GetInterfaceTypes(typeof(T));
    /// <summary>
    /// interfaceType을 Interface 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetInterfaceTypes(Type interfaceType) => Cache.CachedClasses.Where(interfaceType.IsAssignableFrom);

    /// <summary>
    /// T를 Attribute로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetAttributeTypes<T>() where T : Attribute => GetAttributeTypes(typeof(T));
    /// <summary>
    /// attributeType을 Attribute 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetAttributeTypes(Type attributeType) => Cache.CachedClasses.Where(type => type.IsDefined(attributeType, false));

    public static IEnumerable<(T attribute, Type type)> GetAttributeTypeInfos<T>() where T : Attribute => Cache.CachedClasses.Where(type => type.IsDefined<T>()).Select(type => (type.GetCustomAttribute<T>(), type));
    
    #endregion
    
    #region [Attribute]

    /// <summary>
    /// T 를 Attribute 로 가지고 있는 Class Type 의 모든 T Attribute
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<T> GetAttributes<T>() where T : Attribute => GetAttributeTypes<T>().SelectMany(type => type.GetCustomAttributes<T>(false));
    
    #endregion
    
    #region [Enum]
    
    public static bool TryGetAttributeEnumTypes<T>(out IEnumerable<Type> type) where T : Attribute => (type = GetAttributeEnumTypes<T>())?.Any() ?? false;
    public static IEnumerable<Type> GetAttributeEnumTypes<T>() where T : Attribute => Cache.CachedEnums.Where(type => type.IsDefined(typeof(T), false));
    
    public static bool TryGetAttributeEnumTypes(Type type, out IEnumerable<Type> enumerable) => (enumerable = GetAttributeEnumTypes(type))?.Any() ?? false;
    public static IEnumerable<Type> GetAttributeEnumTypes(Type type) => Cache.CachedEnums.Where(enumType => enumType.IsDefined(type, false));

    public static bool TryGetAttributeEnumInfos<T>(out IEnumerable<(T attribute, Type enumType)> infos) where T : Attribute => (infos = GetAttributeEnumInfos<T>())?.Any() ?? false;
    public static IEnumerable<(T attribute, Type enumType)> GetAttributeEnumInfos<T>() where T : Attribute => Cache.CachedEnums.Where(type => type.IsDefined(typeof(T), false)).Select(type => (type.GetCustomAttribute<T>(false), type));

    public static bool TryGetAttributeEnumInfos<T>(Type type, out IEnumerable<(T attribute, Type type)> enumerable) where T : Attribute => (enumerable = GetAttributeEnumInfos<T>(type))?.Any() ?? false; 
    public static IEnumerable<(T attribute, Type type)> GetAttributeEnumInfos<T>(Type type) where T : Attribute => Cache.CachedEnums.Where(enumType => enumType.IsEnumDefined(type)).Select(enumType => (enumType.GetCustomAttribute(type) as T, enumType));
    
    #endregion
}