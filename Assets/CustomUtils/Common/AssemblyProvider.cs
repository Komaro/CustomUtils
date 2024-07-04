using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class AssemblyProvider {

    private static Dictionary<string, SystemAssembly> _cachedSystemAssemblyDic;
    private static Dictionary<string, SystemAssembly> CachedSystemAssemblyDic => _cachedSystemAssemblyDic ??= AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).ToDictionary(assembly => assembly.GetName().Name, assembly => assembly);
    
    private static HashSet<SystemAssembly> _cachedSystemAssemblySet;
    private static HashSet<SystemAssembly> CachedSystemAssemblySet => _cachedSystemAssemblySet ??= CachedSystemAssemblyDic.Values.ToHashSetWithDistinct();

    private static Dictionary<string, UnityAssembly> _cachedUnityAssemblyDic;
    private static Dictionary<string, UnityAssembly> CachedUnityAssemblyDic => _cachedUnityAssemblyDic ??= CompilationPipeline.GetAssemblies().ToDictionary(x => x.name, x => x);

    private static HashSet<UnityAssembly> _cachedUnityAssemblySet;
    private static HashSet<UnityAssembly> CachedUnityAssemblySet => _cachedUnityAssemblySet ??= CachedUnityAssemblySet.ToHashSetWithDistinct();

    #region [System.Reflection.Assembly]
    
    public static bool TryGetSystemAssembly(string assemblyName, out SystemAssembly assembly) => (assembly = GetSystemAssembly(assemblyName)) != null;
    public static SystemAssembly GetSystemAssembly(string assemblyName) => CachedSystemAssemblyDic.TryGetValue(assemblyName, out var assembly) ? assembly : null;
    
    public static Dictionary<string, SystemAssembly> GetSystemAssembly() => CachedSystemAssemblyDic;
    
    #endregion

    #region [UnityEditor.Compilation.Assembly]
    
    public static bool TryGetUnityAssembly(SystemAssembly systemAssembly, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(systemAssembly)) != null;
    public static UnityAssembly GetUnityAssembly(SystemAssembly systemAssembly) => CachedUnityAssemblyDic.TryGetValue(systemAssembly.GetName().Name, out var unityAssembly) ? unityAssembly : null;
    
    public static bool TryGetUnityAssembly(string assemblyName, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(assemblyName)) != null;
    public static UnityAssembly GetUnityAssembly(string assemblyName) => CachedUnityAssemblyDic.TryGetValue(assemblyName, out var unityAssembly) ? unityAssembly : null;
    
    public static Dictionary<string, UnityAssembly> GetUnityAssembly() => CachedUnityAssemblyDic;
   
    #endregion
}