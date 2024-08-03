using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class AssemblyProvider {

    private static class Cache {
        
        #region [System.Reflection.Assembly]
        
        private static Dictionary<string, Assembly> _cachedSystemAssemblyDic;
        public static Dictionary<string, Assembly> CachedSystemAssemblyDic => _cachedSystemAssemblyDic ??= AppDomain.CurrentDomain.GetAssemblies().AsParallel().Where(assembly => assembly.IsDynamic == false && string.IsNullOrEmpty(assembly.Location) == false).ToDictionary(assembly => assembly.GetName().Name, assembly => assembly);
    
        private static HashSet<Assembly> _cachedSystemAssemblySet;
        public static HashSet<Assembly> CachedSystemAssemblySet => _cachedSystemAssemblySet ??= CachedSystemAssemblyDic.Values.ToHashSetWithDistinct();
        
        #endregion
    }
    
    #region [System.Reflection.Assembly]
    
    public static bool TryGetSystemAssembly(string assemblyName, out Assembly assembly) => (assembly = GetSystemAssembly(assemblyName)) != null;
    public static Assembly GetSystemAssembly(string assemblyName) => Cache.CachedSystemAssemblyDic.TryGetValue(assemblyName, out var assembly) ? assembly : null;
    
    public static Dictionary<string, Assembly> GetSystemAssemblyDic() => Cache.CachedSystemAssemblyDic;
    public static HashSet<Assembly> GetSystemAssemblySet() => Cache.CachedSystemAssemblySet;

    #endregion
}