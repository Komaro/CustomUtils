using System;
using System.Collections.Generic;
using System.Linq;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class UnityAssemblyExtension {

    private static readonly HashSet<string> _unityAssemblySet = UnityAssemblyProvider.GetUnityAssemblySet().WhereSelect(assembly => assembly.sourceFiles.Any(path => path.StartsWith(Constants.Folder.ASSETS)), assembly => assembly.name).ToHashSet();

    public static bool IsPackage(this UnityAssembly assembly) => _unityAssemblySet.Contains(assembly.name) == false;
    public static bool IsCustom(this UnityAssembly assembly) => _unityAssemblySet.Contains(assembly.name);
}

public static class SystemAssemblyExtension {

    public static bool TryGetType(this SystemAssembly assembly, string name, out Type type) => (type = assembly.GetType(name)) != null;

    public static bool IsBuiltIn(this SystemAssembly assembly) => assembly.Location.Contains(Constants.Folder.SCRIPT_ASSEMBLIES);
    
    [TempMethod]
    public static bool IsCustom(this SystemAssembly assembly) => assembly.IsBuiltIn();
}