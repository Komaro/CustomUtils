using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

namespace CustomUtils.Common {
    public static class AssemblyProvider {

        private static Dictionary<string, SystemAssembly> _cachedSystemAssemblyDic;
        private static Dictionary<string, SystemAssembly> CachedSystemAssemblyDic => _cachedSystemAssemblyDic ??= AppDomain.CurrentDomain.GetAssemblies().ToDictionary(x => x.GetName().Name, x => x);
        
        private static HashSet<SystemAssembly> _cachedSystemAssemblySet;
        private static HashSet<SystemAssembly> CachedSystemAssemblySet => _cachedSystemAssemblySet ??= CachedSystemAssemblyDic.Values.ToHashSetWithDistinct();

        private static Dictionary<string, UnityAssembly> _cachedUnityAssemblyDic;
        private static Dictionary<string, UnityAssembly> CachedUnityAssemblyDic => _cachedUnityAssemblyDic ??= CompilationPipeline.GetAssemblies().ToDictionary(x => x.name, x => x);

        private static HashSet<UnityAssembly> _cachedUnityAssemblySet;
        internal static HashSet<UnityAssembly> CachedUnityAssemblySet => _cachedUnityAssemblySet ??= CachedUnityAssemblySet.ToHashSetWithDistinct();

        public static bool TryGetSystemAssembly(string assemblyName, out SystemAssembly assembly) => (assembly = GetSystemAssembly(assemblyName)) != null;
        public static SystemAssembly GetSystemAssembly(string assemblyName) => CachedSystemAssemblyDic.TryGetValue(assemblyName, out var assembly) ? assembly : null;

        public static bool TryGetUnityAssembly(SystemAssembly systemAssembly, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(systemAssembly)) != null;
        public static UnityAssembly GetUnityAssembly(SystemAssembly systemAssembly) => CachedUnityAssemblyDic.TryGetValue(systemAssembly.GetName().Name, out var unityAssembly) ? unityAssembly : null;
        
        public static bool TryGetUnityAssembly(string assemblyName, out UnityAssembly unityAssembly) => (unityAssembly = GetUnityAssembly(assemblyName)) != null;
        public static UnityAssembly GetUnityAssembly(string assemblyName) => CachedUnityAssemblyDic.TryGetValue(assemblyName, out var unityAssembly) ? unityAssembly : null;
    }
}