using System.Collections.Immutable;
using UnityEditor.Compilation;
using UnityAssembly = UnityEditor.Compilation.Assembly;
using SystemAssembly = System.Reflection.Assembly;

public static class UnityAssemblyProvider {

    private static class Cache {

        #region [UnityEditor.Comiplation.Assembly]

        private static ImmutableDictionary<string, UnityAssembly> _cachedUnityAssemblyDic;
        public static ImmutableDictionary<string, UnityAssembly> CachedUnityAssemblyDic => _cachedUnityAssemblyDic ??= CompilationPipeline.GetAssemblies().ToImmutableDictionary(assembly => assembly.name, assembly => assembly);

        private static ImmutableHashSet<UnityAssembly> _cachedUnityAssemblySet;
        public static ImmutableHashSet<UnityAssembly> CachedUnityAssemblySet => _cachedUnityAssemblySet ??= CachedUnityAssemblyDic.Values.ToImmutableHashSetWithDistinct();
        
        #endregion
    }
    
    #region [UnityEditor.Comiplation.Assembly]
    
    public static bool TryGetUnityAssembly(SystemAssembly systemAssembly, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(systemAssembly)) != null;
    public static UnityAssembly GetUnityAssembly(SystemAssembly systemAssembly) => Cache.CachedUnityAssemblyDic.TryGetValue(systemAssembly.GetName().Name, out var unityAssembly) ? unityAssembly : null;
    public static bool TryGetUnityAssembly(string assemblyName, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(assemblyName)) != null;
    public static UnityAssembly GetUnityAssembly(string assemblyName) => Cache.CachedUnityAssemblyDic.TryGetValue(assemblyName, out var unityAssembly) ? unityAssembly : null;
    public static ImmutableDictionary<string, UnityAssembly> GetUnityAssemblyDic() => Cache.CachedUnityAssemblyDic;
    public static ImmutableHashSet<UnityAssembly> GetUnityAssemblySet() => Cache.CachedUnityAssemblySet;
    
    #endregion
}