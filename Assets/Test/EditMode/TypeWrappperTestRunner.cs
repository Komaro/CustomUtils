using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

[Category(TestConstants.Category.REFLECTION)]
[Category(TestConstants.Category.ANALYZER)]
public class TypeWrapperTestRunner {
    
    [Test]
    public void TypeWrappingTest() {
        var assemblyList = AssemblyProvider.GetSystemAssemblySet().Where(assembly => assembly.IsDynamic == false && assembly.IsCustom()).ToImmutableDictionary(assembly => assembly, assembly => assembly.ExportedTypes.ToImmutableList());
        Assert.IsNotEmpty(assemblyList);
        foreach (var (systemAssembly, typeList) in assemblyList) {
            var filterList = typeList.Where(type => type.IsSubclassOf(typeof(DiagnosticAnalyzer)) && type.IsDefined<ObsoleteAttribute>() == false).ToImmutableList();
            if (filterList.Any() && UnityAssemblyProvider.TryGetUnityAssembly(systemAssembly, out var unityAssembly)) {
                var compilation = CSharpCompilation.Create($"TempAssembly_{unityAssembly.name}")
                    .AddSyntaxTrees(unityAssembly.sourceFiles.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).WithFilePath(Path.Combine(Constants.Path.PROJECT_PATH, path))))
                    .AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));
                
                foreach (var type in filterList) {
                    INamedTypeSymbol symbol;
                    if (type.IsGenericTypeDefinition) {
                        symbol = compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().FullName ?? string.Empty);
                    } else if (type.IsGenericType) {
                        symbol = compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().MakeGenericType(type.GenericTypeArguments).FullName ?? string.Empty);
                    } else {
                        symbol = compilation.GetTypeByMetadataName(type.FullName ?? string.Empty);
                    }
                    
                    if (symbol != null) {
                        var location = symbol.Locations.First();
                        Logger.TraceLog($"{type.FullName} || {location.GetFilePath()} || {location.GetLinePosition()}");
                        foreach (var member in symbol.GetMembers()) {
                            if (member is IMethodSymbol methodSymbol) {
                                Logger.TraceLog(member.Name);
                            }
                        }
                        
                        foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                            
                        }
                    }
                }
            }
        }
    }
}

[Temp]
public ref struct TypeWrapper {

    public Type Type { get; private set; }

    public TypeWrapper(Type type) {
        Type = type;
    }
    
    public (string path, int line) GetLocation() {
        return (default, default);
    }
}

[Temp]
public ref struct MemberWrapper {

    public MemberInfo Info { get; private set; }

    public MemberWrapper(MemberInfo info) {
        Info = info;
    }

    public (string path, int line) GetLocation() {
        return (default, default);
    }
}