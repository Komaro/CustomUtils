using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

public static class AssemblyProvider {

    private static class Cache {
        
        #region [System.Reflection.Assembly]

        private static ImmutableDictionary<string, Assembly> _cachedSystemAssemblyDic;
        public static ImmutableDictionary<string, Assembly> CachedSystemAssemblyDic => _cachedSystemAssemblyDic ??= AppDomain.CurrentDomain.GetAssemblies().AsParallel().Where(assembly => assembly.IsDynamic == false && string.IsNullOrEmpty(assembly.Location) == false).ToImmutableDictionaryWithDistinct(assembly => assembly.GetName().Name, assembly => assembly);

        private static ImmutableHashSet<Assembly> _cachedSystemAssemblySet;
        public static ImmutableHashSet<Assembly> CachedSystemAssemblySet => _cachedSystemAssemblySet ??= CachedSystemAssemblyDic.Values.ToImmutableHashSetWithDistinct();
        
        #endregion
    }
    
    #region [System.Reflection.Assembly]
    
    public static bool TryGetSystemAssembly(string assemblyName, out Assembly assembly) => (assembly = GetSystemAssembly(assemblyName)) != null;
    public static Assembly GetSystemAssembly(string assemblyName) => Cache.CachedSystemAssemblyDic.TryGetValue(assemblyName, out var assembly) ? assembly : null;
    
    public static ImmutableDictionary<string, Assembly> GetSystemAssemblyDic() => Cache.CachedSystemAssemblyDic;
    public static ImmutableHashSet<Assembly> GetSystemAssemblySet() => Cache.CachedSystemAssemblySet;

    #endregion
}