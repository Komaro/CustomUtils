using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityAssembly = UnityEditor.Compilation.Assembly;
using SystemAssembly = System.Reflection.Assembly;

public partial class EditorTypeLocationService {

// #if UNITY_2022_3_OR_NEWER
    
    private static int _taskId;
    
    private static readonly ConcurrentDictionary<Type, (string path, int line)> _typeLocationDic = new();
    private static readonly ConcurrentDictionary<MemberInfo, (string path, int line)> _memberInfoLocationDic = new();

    private static readonly CancellationTokenSource _tokenSource = new();
    
    public static bool IsRunning { get; private set; }

    private static async void AnalyzeTypeLocation(bool isForceAnalyze = false) {
        if (IsRunning) {
            Logger.TraceLog("Already analyzing type location", Color.Yellow);
            if (Progress.Cancel(_taskId) == false) {
                Logger.TraceError($"{_taskId} {nameof(Progress)} cancellation failed");
                return;
            }
            
            _tokenSource.Cancel();
        }
        
        try {
            if (isForceAnalyze) {
                _typeLocationDic.Clear();
            }
            
            _taskId = Progress.Start("Type Location Cache");
            IsRunning = true;

            EditorPrefsUtil.TryGet(SerializedAssemblyArray.IGNORE_ASSEMBLIES_KEY, out string[] ignoreAssemblies);
            var ignoreAssemblySet = ignoreAssemblies.ToHashSetWithDistinct();
            var assemblyDic = UnityAssemblyProvider.GetUnityAssemblySet().Where(assembly => assembly.IsCustom() && ignoreAssemblySet.Contains(assembly.name) == false).ToDictionary(assembly => assembly, assembly => assembly.sourceFiles);
            try {
                var processorCount = _isActiveFullProcessor ? Environment.ProcessorCount : Environment.ProcessorCount / 2;
                await Task.Run(() => assemblyDic.AsParallel().WithDegreeOfParallelism(processorCount).WithCancellation(_tokenSource.Token).ForAll(pair => AnalyzeTypeLocation(pair.Key, pair.Value, _tokenSource.Token)), _tokenSource.Token);
            } catch (Exception ex) {
                Logger.TraceError(ex);
            } finally {
                Progress.Remove(_taskId);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            IsRunning = false;
        }
    }

    [Obsolete]
    private static void AnalyzeTypeLocation(UnityAssembly assembly, string[] sourceFiles, CancellationToken token) {
        var assemblyId = Progress.Start($"{assembly.name} Type location cache", parentId:_taskId);
        var parsingId = Progress.Start($"{assembly.name} Parsing...", parentId: assemblyId);
        var syntaxTreeList = new List<SyntaxTree>();
        foreach (var path in sourceFiles) {
            token.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(Constants.Path.PROJECT_PATH, path);
            syntaxTreeList.Add(CSharpSyntaxTree.ParseText(File.ReadAllTextAsync(fullPath, token).Result, cancellationToken: token).WithFilePath(fullPath));
            Progress.Report(parsingId, syntaxTreeList.Count, sourceFiles.Length);
        }

        Progress.Remove(parsingId);

        var compilationId = Progress.Start($"{assembly.name} Compilation...", parentId:assemblyId);
        var compilation = CSharpCompilation.Create($"{assembly.name}_Temp")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTreeList);

        Progress.Remove(compilationId);
        
        if (AssemblyProvider.TryGetSystemAssembly(assembly.name, out var systemAssembly)) {
            var analyzingId = Progress.Start($"{assembly.name} Analyzing...", parentId:assemblyId);
            var cachedSourceCount = 0;
            foreach (var syntaxTree in syntaxTreeList) {
                token.ThrowIfCancellationRequested();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                foreach (var syntax in syntaxTree.GetRootAsync(token).Result.DescendantNodes().OfType<TypeDeclarationSyntax>()) {
                    token.ThrowIfCancellationRequested();
                    if (semanticModel.TryGetDeclaredSymbol(syntax, out var symbol)) {
                        if (TryGetType(symbol, systemAssembly, out var type) == false) {
                            continue;
                        }
                        
                        if (_typeLocationDic.ContainsKey(type)) {
                            continue;
                        }

                        var location = symbol.Locations.First();
                        _typeLocationDic.TryAdd(type, (location.GetFilePath(), location.GetLinePosition()));
                    }
                }
            
                cachedSourceCount++;
                Progress.Report(analyzingId, cachedSourceCount, sourceFiles.Length);
            }

            Progress.Remove(analyzingId);
        }
        
        Progress.Remove(assemblyId);
    }
    
    private static void AnalyzeMemberInfoLocation(UnityAssembly assembly, string[] sourceFiles, CancellationToken token) {
        if (AssemblyProvider.TryGetSystemAssembly(assembly.name, out var systemAssembly) == false) {
            return;
        }

        var typeCache = new Dictionary<string, Type>();
        var fieldCache = new Dictionary<string, FieldInfo>();
        var propertyCache = new Dictionary<string, PropertyInfo>();
        var methodCache = new Dictionary<string, MethodInfo>();
        var typeList = systemAssembly.GetTypes().Where(type => type.IsDefined<CompilerGeneratedAttribute>() == false).ToList();
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        foreach (var type in typeList) {
            typeCache.AutoAdd(GetFixName(type), type);
            
            foreach (var fieldInfo in type.GetFields(bindingFlags)) {
                if (fieldInfo.FieldType.IsDefined<CompilerGeneratedAttribute>()) {
                    continue;
                }
                
                fieldCache.AutoAdd(GetFixName(fieldInfo), fieldInfo);
            }

            foreach (var propertyInfo in type.GetProperties(bindingFlags)) {
                propertyCache.AutoAdd(GetFixName(propertyInfo), propertyInfo);
            }

            foreach (var methodInfo in type.GetMethods(bindingFlags)) {
                methodCache.AutoAdd(GetFixName(methodInfo), methodInfo);
            }
        }

        var syntaxTreeList = new List<SyntaxTree>();
        foreach (var path in assembly.sourceFiles) {
            var fullPath = Path.Combine(Constants.Path.PROJECT_PATH, path);
            syntaxTreeList.Add(CSharpSyntaxTree.ParseText(File.ReadAllTextAsync(fullPath, token).Result, CSharpParseOptions.Default.WithPreprocessorSymbols(assembly.defines), cancellationToken: token).WithFilePath(fullPath));
        }

        var compilation = CSharpCompilation.Create($"{assembly.name}_Temp")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTreeList);
        
        foreach (var syntaxTree in syntaxTreeList) {
            token.ThrowIfCancellationRequested();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var syntax in syntaxTree.GetRootAsync(token).Result.DescendantNodes()) {
                switch (syntax) {
                    case TypeDeclarationSyntax typeSyntax:
                        if (semanticModel.TryGetDeclaredSymbol(typeSyntax, out var typeSymbol) == false) {
                            Logger.TraceLog(typeSyntax.GetLocation().ToString());
                            continue;
                        }

                        if (typeCache.TryGetValue(GetFixName(typeSymbol), out var type) && typeSymbol.Locations.TryFirst(out var typeLocation)) {
                            // TODO. Add duplicate check
                            _memberInfoLocationDic.AutoAdd(type, typeLocation.GetRedirectLocation());
                        }
                        break;
                    case FieldDeclarationSyntax fieldSyntax:
                        var variable = fieldSyntax.Declaration.Variables.FirstOrDefault();
                        if (variable == null || semanticModel.TryGetDeclaredSymbol(variable, out var fieldSymbol) == false) {
                            Logger.TraceLog(fieldSyntax.GetLocation().ToString());
                            continue;
                        }
                        
                        if (fieldCache.TryGetValue(GetFixName(fieldSymbol), out var fieldInfo) && fieldSymbol.Locations.TryFirst(out var fieldLocation)) {
                            // TODO. Add duplicate check
                            _memberInfoLocationDic.AutoAdd(fieldInfo, fieldLocation.GetRedirectLocation());
                        }
                        break;
                    case PropertyDeclarationSyntax propertySyntax:
                        if (semanticModel.TryGetDeclaredSymbol(propertySyntax, out var propertySymbol) == false) {
                            Logger.TraceLog(propertySyntax.GetLocation().ToString());
                            continue;
                        }

                        if (propertyCache.TryGetValue(GetFixName(propertySymbol), out var propertyInfo) && propertySymbol.Locations.TryFirst(out var propertyLocation)) {
                            // TODO. Add duplicate check
                            _memberInfoLocationDic.AutoAdd(propertyInfo, propertyLocation.GetRedirectLocation());
                        }
                        break;
                    case MethodDeclarationSyntax methodSyntax:
                        if (semanticModel.TryGetDeclaredSymbol(methodSyntax, out var methodSymbol) == false) {
                            Logger.TraceLog(methodSyntax.GetLocation().ToString());
                            continue;
                        }
                        
                        if (methodCache.TryGetValue(GetFixName(methodSymbol), out var methodInfo) && methodSymbol.Locations.TryFirst(out var methodLocation)) {
                            // TODO. Add duplicate check
                            _memberInfoLocationDic.AutoAdd(methodInfo, methodLocation.GetRedirectLocation());
                        }
                        break;
                }
            }
        }
    }
    
    private static string GetFixName(Type type) => type.FullName;

    private static string GetFixName(FieldInfo info) {
        using (StringUtil.StringBuilderPool.Get(out var builder)) {
            if (info.DeclaringType != null) {
                builder.Append(GetFixName(info.DeclaringType));
                builder.Append("+");
            }
            
            builder.Append(info.Name);
            return builder.ToString();
        }
    }

    private static string GetFixName(PropertyInfo info) {
        using (StringUtil.StringBuilderPool.Get(out var builder)) {
            if (info.DeclaringType != null) {
                builder.Append(GetFixName(info.DeclaringType));
                builder.Append("+");
            }

            builder.Append(info.Name);
            return builder.ToString();
        }
    }

    private static string GetFixName(MethodInfo info) {
        using (StringUtil.StringBuilderPool.Get(out var builder)) {
            if (info.DeclaringType != null) {
                builder.Append(GetFixName(info.DeclaringType));
                builder.Append("+");
            }

            builder.Append(info.Name);
            return builder.ToString();
        }
    }
    
    [Obsolete]
    private static bool TryGetType(ISymbol symbol, SystemAssembly assembly, out Type type) {
        using (StringUtil.StringBuilderConcurrentPool.Get(out var builder)) {
            if (symbol.ContainingNamespace.IsGlobalNamespace == false) {
                builder.Append($"{symbol.ContainingNamespace.ToDisplayString()}.");
            }

            AppendNestedType(builder, symbol);
            
            return assembly.TryGetType(builder.ToString(), out type);
        }
    }
    
    private static string GetFixName(ISymbol symbol) {
        using (StringUtil.StringBuilderConcurrentPool.Get(out var builder)) {
            if (symbol.ContainingNamespace.IsGlobalNamespace == false) {
                builder.Append($"{symbol.ContainingNamespace.ToDisplayString()}.");
            }

            AppendNestedType(builder, symbol);
            return builder.ToString();
        }
    }

    private static void AppendNestedType(StringBuilder builder, ISymbol symbol) {
        if (symbol.ContainingType != null) {
            AppendNestedType(builder, symbol.ContainingType);
            builder.Append("+");
        }
        
        builder.Append(symbol.MetadataName);
    }
}