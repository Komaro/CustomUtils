using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class UnityAssemblyExtension {

    private static readonly HashSet<string> _unityAssemblySet = UnityAssemblyProvider.GetUnityAssemblySet().WhereSelect(assembly => assembly.sourceFiles.Any(path => path.StartsWith(Constants.Folder.ASSETS)), assembly => assembly.name).ToHashSet();

    public static bool IsPackage(this UnityAssembly assembly) => _unityAssemblySet.Contains(assembly.name) == false;
    public static bool IsCustom(this UnityAssembly assembly) => _unityAssemblySet.Contains(assembly.name);
}

public static class SystemAssemblyExtension {

    [Obsolete("정확도 낮음")]
    public static bool TryGetType(this SystemAssembly assembly, string name, out Type type) => (type = assembly.GetType(name)) != null;

    public static bool IsPackage(this SystemAssembly assembly) => UnityAssemblyProvider.GetUnityAssembly(assembly)?.IsPackage() ?? false;
    public static bool IsCustom(this SystemAssembly assembly) => UnityAssemblyProvider.GetUnityAssembly(assembly)?.IsCustom() ?? false;
}