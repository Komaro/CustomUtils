using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

[RefactoringRequired("구조적인 문제점으로 인해 정확도를 보장할 수 없음. 근본적인 해결이 필요할 것으로 보임.")]
public static class AnalyzerGenerator {

    private static readonly Regex PLUGINS_FOLDER = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    // TODO. 일단 정확도가 낮은 상태로 사용하고 이후 좀더 정확도를 높일 필요가 있음
    // TODO. 소스 파일 경로를 획득할 완전하게 새로운 방식 필요
    [Obsolete]
    public static void GenerateCustomAnalyzerDll(string dllName, IEnumerable<Type> types) {
        // TODO. EditorTypeLocationService가 비활성화 되어 있는 경우 예외처리
        // var typePaths = types.SelectMany(type => type.GetAllTypes()).Where(EditorTypeLocationService.ContainsTypeLocation).Select(type => EditorTypeLocationService.GetTypeLocation(type).path).ToArray();
        // if (typePaths.Any() == false) {
        //     Logger.TraceError($"{nameof(typePaths)} is empty. Please review the implementation through inheriting {nameof(DiagnosticAnalyzer)} again.");
        //     return;
        // }
        //
        // GenerateCustomAnalyzerDllOnCompilation(dllName, typePaths.Distinct());

        // todo. 정확도 낮음
        var typePathDic = new Dictionary<Type, string>();
        foreach (var type in types) {
            // if (UnityAssemblyProvider.TryGetSourceFilePath(type, out var path)) {
            //     typePathDic.AutoAdd(type, path);
            //     foreach (var baseType in type.GetBaseTypes()) {
            //         if (typePathDic.ContainsKey(baseType) == false && UnityAssemblyProvider.TryGetSourceFilePath(baseType, out path)) {
            //             typePathDic.AutoAdd(baseType, path);
            //         }
            //     }
            // }
        }

        // SRP에 맞지 않음. 애초에 이전 단계에서 필텅링 되었어야 함.
        // if (typePathDic.Count <= 0) {
        //     Logger.TraceLog($"{nameof(typePathDic)} is empty. Building all Analyzers that inherit from {nameof(DiagnosticAnalyzer)}.", Color.yellow);
        //     typePathDic = ReflectionProvider.GetSubTypesOfType<DiagnosticAnalyzer>().Where(type => PLUGINS_FOLDER.IsMatch(type.Assembly.Location) == false && type.IsDefined<ObsoleteAttribute>() == false).ToDictionary(type => type, UnityAssemblyProvider.GetSourceFilePath);
        // }

        if (typePathDic.Any() == false) {
            Logger.TraceError($"{nameof(typePathDic)} is empty. Please review the implementation through inheriting {nameof(DiagnosticAnalyzer)} again.");
            return;
        }
        
        GenerateCustomAnalyzerDllOnCompilation(dllName, typePathDic.Values.ToArray());
    }
    
    private static void GenerateCustomAnalyzerDllOnCompilation(string dllName, IEnumerable<string> sourceFiles) {
        if (string.IsNullOrEmpty(dllName)) {
            dllName = Constants.Analyzer.ANALYZER_PLUGIN_NAME;
        }
        
        var outputPath = $"{Constants.Path.PROJECT_TEMP_PATH}/{dllName.AutoSwitchExtension(Constants.Extension.DLL)}";
        var syntaxTreeList = new List<SyntaxTree>();
        foreach (var path in sourceFiles) {
            // if (SystemUtil.TryReadAllText(Path.Combine(Constants.Path.PROJECT_PATH, path), out var source)) {
            if (IOUtil.TryReadText(path, out var source)) {
                syntaxTreeList.Add(SyntaxFactory.ParseSyntaxTree(SourceText.From(source), CSharpParseOptions.Default.WithPreprocessorSymbols()));
            }
        }
        
        var compilation = CSharpCompilation.Create(dllName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)))
            .AddSyntaxTrees(syntaxTreeList.ToArray());

        OnStart(outputPath);
        var result = compilation.Emit(outputPath);
        if (result.Success) {
            OnFinish(outputPath, compilation.Assembly.TypeNames.ToArray(name => new CompilerMessage {
                type = CompilerMessageType.Info,
                message = name,
            }));
        } else { 
            Logger.TraceError($"Compilation Failed || {outputPath}");
            OnFailed(result);
        }
    }

#if UNITY_6000_0_OR_NEWER == false
    private static void GenerateCustomAnalyzerDllOnAssemblyBuilder(IEnumerable<string> sourceFiles) {
        if (UnityAssemblyProvider.TryGetUnityAssembly(SystemAssembly.GetExecutingAssembly(), out var assembly)) {
            try {
                var outputPath = $"{Constants.Path.PROJECT_TEMP_PATH}/{Constants.Analyzer.ANALYZER_NAME}{Constants.Extension.DLL}";
                var builder = new AssemblyBuilder(outputPath, sourceFiles) {
                    additionalReferences = assembly.assemblyReferences.Select(x => x.outputPath).ToArray(),
                };
                
                builder.buildStarted += OnStart;
                builder.buildFinished += OnFinish;
                builder.Build();
            } catch (Exception ex) {
                Logger.TraceError(ex);
                throw;
            }
        }
    }
#endif
    
    private static void OnStart(string assemblyPath) => Logger.TraceLog($"Start {Path.GetFileName(assemblyPath)} {nameof(UnityAssembly)} build", Color.green);

    private static void OnFinish(string outputPath, CompilerMessage[] messages) {
        Logger.TraceLog($"Build Output || {outputPath}", Color.yellow);
        foreach (var message in messages) {
            switch (message.type) {
                case CompilerMessageType.Error:
                    Logger.TraceError(message.message);
                    break;
                case CompilerMessageType.Warning:
                    Logger.TraceWarning(message.message);
                    break;
                default:
                    Logger.TraceLog(message.message);
                    break;
            }
        }
        
        File.WriteAllText(Path.ChangeExtension(outputPath, ".log"), messages.ToStringCollection(x => x.message, "\n"));
        AnalyzerBuildPostProcess(outputPath);
    }

    private static void OnFailed(EmitResult result) {
        foreach (var diagnostic in result.Diagnostics) {
            switch (diagnostic.Severity) {
                case DiagnosticSeverity.Warning:
                    Logger.TraceWarning(diagnostic);
                    break;
                case DiagnosticSeverity.Error:
                    Logger.TraceError($"{diagnostic.Id} || {diagnostic.Location} || {diagnostic.GetMessage()}");
                    break;
                default:
                    Logger.TraceLog(diagnostic);
                    break;
            }
        }
    }
    
    private static void AnalyzerBuildPostProcess(string outputPath) {
        var copyPath = Path.Combine(Constants.Path.PLUGINS_FULL_PATH, Path.GetFileName(outputPath));
        SystemUtil.MoveFile(outputPath, copyPath);
        File.Delete(outputPath.AutoSwitchExtension(Constants.Extension.PDB));
        
        var assetPath = copyPath.GetAfter(Constants.Folder.ASSETS, true);
        if (AssetImporter.GetAtPath(assetPath) is PluginImporter importer) {
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            importer.SaveAndReimport();
        }
        
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        
        if (AssetDatabaseUtil.TryLoad(copyPath, out var dllAsset)) {
            var labels = AssetDatabase.GetLabels(dllAsset);
            Logger.TraceLog(labels.ToStringCollection(", "));
            if (labels.Contains(Constants.Analyzer.ROSLYN_ANALYZER_LABEL) == false) {
                var newLabels = labels.Union(new[] { Constants.Analyzer.ROSLYN_ANALYZER_LABEL }).ToArray();
                AssetDatabase.SetLabels(dllAsset, newLabels);
            }
        } else {
            Logger.TraceError($"Not found dll asset");
        }
    }
}