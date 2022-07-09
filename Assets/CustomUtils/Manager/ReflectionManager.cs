using System;
using System.Collections.Generic;
using System.Linq;

public static class ReflectionManager
{
    private static IEnumerable<Type> _cachedClasses;
    private static IEnumerable<Type> CacheClassTypes() => AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).SelectMany(assembly => assembly.GetExportedTypes()).Where(type => type.IsClass && type.IsAbstract == false);
    public static IEnumerable<Type> GetTypes() => _cachedClasses ??= CacheClassTypes();
    public static IEnumerable<Type> GetSubClassTypes<T>() where T : class => GetTypes().Where(type => type.IsSubclassOf(typeof(T))); // T 를 SubClass 로 가지는 ClassType
    public static IEnumerable<Type> GetInterfaceTypes<T>() => GetTypes().Where(type => typeof(T).IsAssignableFrom(type)); // T 를 Interface 로 가지는 ClassType
    public static IEnumerable<Type> GetAttributeTypes<T>() where T : Attribute => GetTypes().Where(type => type.IsDefined(typeof(T), false)); // T 를 Attribute 로 가지는 ClassType
    public static IEnumerable<T> GetAttribute<T>() where T : Attribute => GetAttributeTypes<T>().Select(type => type.GetCustomAttributes(typeof(T), false).FirstOrDefault()).Cast<T>(); // T 를 Attribute 로 가지고 있는 Class Type 의 T Attribute
    public static IEnumerable<T[]> GetAllAttributes<T>() where T : Attribute => GetAttributeTypes<T>().Select(type => type.GetCustomAttributes(typeof(T), false)).Cast<T[]>(); // T 를 Attribute 로 가지고 있는 Class Type 의 T Attribute List
}