using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityAssembly = UnityEditor.Compilation.Assembly;
using SystemAssembly = System.Reflection.Assembly;

public static class UnityAssemblyProvider {

    private static class Cache {

        #region [UnityEditor.Comiplation.Assembly]
        
        private static Dictionary<string, UnityAssembly> _cachedUnityAssemblyDic;
        public static Dictionary<string, UnityAssembly> CachedUnityAssemblyDic => _cachedUnityAssemblyDic ??= CompilationPipeline.GetAssemblies().ToDictionary(x => x.name, x => x);

        private static HashSet<UnityAssembly> _cachedUnityAssemblySet;
        public static HashSet<UnityAssembly> CachedUnityAssemblySet => _cachedUnityAssemblySet ??= CachedUnityAssemblyDic.Values.ToHashSetWithDistinct();
        
        #endregion
        
        #region [Source File(Ignore Builtin Assembly)]
        
        private static Dictionary<string, string> _cachedSourceFilePathDic;
        public static Dictionary<string, string> CachedSourceFilePathDic =>  _cachedSourceFilePathDic ??= CachedUnityAssemblySet.Where(assembly => assembly.IsBuiltin() == false).SelectMany(assembly => assembly.sourceFiles).ToDictionaryWithDistinct(Path.GetFileNameWithoutExtension, path => path);
        
        private static Dictionary<Type, string> _cachedSourceFileTypePathDic;
        public static Dictionary<Type, string> CachedSourceFileTypePathDic => _cachedSourceFileTypePathDic ??= ReflectionProvider.GetCachedTypes().Where(type => CachedSourceFilePathDic.ContainsKey(type.Name)).ToDictionary(type => type, type => CachedSourceFilePathDic[type.Name]);

        #endregion
    }
    
    #region [UnityEditor.Comiplation.Assembly]
    
    public static bool TryGetUnityAssembly(SystemAssembly systemAssembly, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(systemAssembly)) != null;
    public static UnityAssembly GetUnityAssembly(SystemAssembly systemAssembly) => Cache.CachedUnityAssemblyDic.TryGetValue(systemAssembly.GetName().Name, out var unityAssembly) ? unityAssembly : null;
    public static bool TryGetUnityAssembly(string assemblyName, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(assemblyName)) != null;
    public static UnityAssembly GetUnityAssembly(string assemblyName) => Cache.CachedUnityAssemblyDic.TryGetValue(assemblyName, out var unityAssembly) ? unityAssembly : null;
    public static Dictionary<string, UnityAssembly> GetUnityAssemblyDic() => Cache.CachedUnityAssemblyDic;
    public static HashSet<UnityAssembly> GetUnityAssemblySet() => Cache.CachedUnityAssemblySet;
    
    #endregion

    #region [Source File]
    
    public static Dictionary<string, string> GetSourceFilePathDic() => Cache.CachedSourceFilePathDic;
    public static Dictionary<Type, string> GetSourceFiledTypePathDic() => Cache.CachedSourceFileTypePathDic;

    public static bool TryGetSourceFilePath(Type type, out string path) => string.IsNullOrEmpty(path = GetSourceFilePath(type)) == false;
    public static string GetSourceFilePath(Type type) => Cache.CachedSourceFileTypePathDic.TryGetValue(type, out var path) ? path : string.Empty;

    #endregion
}