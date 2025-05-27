using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
        
        #region [Source File(Ignore Builtin Assembly)]
        
        private static ImmutableDictionary<string, string> _cachedSourceFilePathDic;
        public static ImmutableDictionary<string, string> CachedSourceFilePathDic =>  _cachedSourceFilePathDic ??= CachedUnityAssemblySet.Where(assembly => assembly.IsBuiltin() == false).SelectMany(assembly => assembly.sourceFiles).ToImmutableDictionaryWithDistinct(Path.GetFileNameWithoutExtension, path => path);
        
        private static ImmutableDictionary<Type, string> _cachedSourceFileTypePathDic;
        public static ImmutableDictionary<Type, string> CachedSourceFileTypePathDic => _cachedSourceFileTypePathDic ??= ReflectionProvider.GetTypes().Where(type => CachedSourceFilePathDic.ContainsKey(type.Name)).ToImmutableDictionary(type => type, type => CachedSourceFilePathDic[type.Name]);

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

    #region [Source File]
    
    public static ImmutableDictionary<string, string> GetSourceFilePathDic() => Cache.CachedSourceFilePathDic;
    public static ImmutableDictionary<Type, string> GetSourceFiledTypePathDic() => Cache.CachedSourceFileTypePathDic;

    public static bool TryGetSourceFilePath(Type type, out string path) => string.IsNullOrEmpty(path = GetSourceFilePath(type)) == false;
    public static string GetSourceFilePath(Type type) => Cache.CachedSourceFileTypePathDic.TryGetValue(type, out var path) ? path : string.Empty;

    #endregion
}