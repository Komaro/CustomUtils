using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var assemblyDic = UnityAssemblyProvider.GetUnityAssemblySet().Where(assembly => UnityAssemblyExtension.IsCustom(assembly) && ignoreAssemblySet.Contains(assembly.name) == false).ToDictionary(assembly => assembly, assembly => assembly.sourceFiles);
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
    
    private static bool TryGetType(ISymbol symbol, SystemAssembly assembly, out Type type) {
        using (StringUtil.StringBuilderConcurrentPool.Get(out var builder)) {
            if (symbol.ContainingNamespace.IsGlobalNamespace == false) {
                builder.Append($"{symbol.ContainingNamespace.ToDisplayString()}.");
            }

            AppendNestedType(builder, symbol);
            
            return assembly.TryGetType(builder.ToString(), out type);
        }
    }

    private static void AppendNestedType(StringBuilder builder, ISymbol symbol) {
        if (symbol.ContainingType != null) {
            AppendNestedType(builder, symbol.ContainingType);
            builder.Append("+");
        }
        
        builder.Append(symbol.MetadataName);
    }

    // TODO. Type 뿐만이 아니라 되도록이면 Memoberinfo에 대한 전체 획득을 할 수 있도록 개선 
    private static void AnalyzeMemberInfoLocation(UnityAssembly assembly, string[] sourceFiles, CancellationToken token) {
        // TODO. 우선적으로 필요한 MemberInfo를 특정 string 포멧에 맞춰 캐싱...
        
        // TODO. Symbol 분석...
        
        // TODO. 쓸모없는 캐시 데이터 정리...
    }
    
    
// #else
//
//     // Coroutine
//         
//     public static partial class EditorTypeLocationService {
//         
//     }
//     
// #endif
    
}