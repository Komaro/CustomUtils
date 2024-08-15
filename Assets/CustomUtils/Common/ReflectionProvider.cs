using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionProvider {
    
    private static class Cache {
        
        private static IEnumerable<Type> _cachedTypes;
        public static IEnumerable<Type> CachedTypes => _cachedTypes ??= AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes());
    
        private static IEnumerable<Type> _cachedClasses;
        public static IEnumerable<Type> CachedClasses => _cachedClasses ??= CachedTypes.Where(type => type.IsClass);

        private static IEnumerable<Type> _cachedEnums;
        public static IEnumerable<Type> CachedEnums => _cachedEnums ??= CachedTypes.Where(type => type.IsEnum);
    }
    
    #region [Class]

    public static IEnumerable<Type> GetCachedTypes() => Cache.CachedTypes;

    /// <summary>
    /// T 와 동일한 ClassType
    /// </summary>
    public static Type GetClassType<T>() where T : class => Cache.CachedClasses.FirstOrDefault(type => typeof(T) == type);
    
    /// <summary>
    /// T를 SubClass(상속 관계) 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetSubClassTypes<T>() where T : class => GetSubClassTypes(typeof(T));
    /// <summary>
    /// subClassType을 SubClass(상속 관계) 로 가지는 ClassType
    /// </summary>
    public static IEnumerable<Type> GetSubClassTypes(Type subClassType) => Cache.CachedClasses.Where(type => type.IsSubclassOf(subClassType));

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
    
    #endregion

    
    #region [Attribute]

    /// <summary>
    /// T 를 Attribute 로 가지고 있는 Class Type 의 모든 T Attribute
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<T> GetAttributes<T>() where T : Attribute => GetAttributeTypes<T>().SelectMany(type => type.GetCustomAttributes<T>(false));
    
    #endregion

    
    #region [Enum]

    public static IEnumerable<Type> GetAttributeEnumTypes<T>() where T : Attribute => Cache.CachedEnums.Where(type => type.IsDefined(typeof(T), false));

    public static bool TryGetAttributeEnumTypes<T>(out IEnumerable<Type> type) where T : Attribute {
        type = GetAttributeEnumTypes<T>();
        return type?.Any() ?? false;
    }

    public static IEnumerable<(T attribute, Type enumType)> GetAttributeEnumInfos<T>() where T : Attribute {
        return Cache.CachedEnums.Where(type => type.IsDefined(typeof(T), false)).Select(type => (type.GetCustomAttribute<T>(false), type));
    }

    public static bool TryGetAttributeEnumInfos<T>(out IEnumerable<(T attribute, Type enumType)> infos) where T : Attribute {
        infos = GetAttributeEnumInfos<T>();
        return infos != null && infos.Any();
    }

    #endregion
}