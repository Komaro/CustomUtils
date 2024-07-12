using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class AnalyzerGenerator {

    private static readonly Regex PLUGINS_FOLDER = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    public static void GenerateCustomAnalyzerDll(string dllName, IEnumerable<Type> typeEnumerable) {
        var pathList = new List<string>();
        foreach (var type in typeEnumerable) {
            if (AssemblyProvider.TryGetSourceFilePath(type, out var path)) {
                pathList.Add(path);
            }
        }

        if (pathList.Count <= 0) {
            Logger.TraceLog($"{nameof(pathList)} is empty. Building all Analyzers that inherit from {nameof(DiagnosticAnalyzer)}.", Color.yellow);
            pathList = ReflectionProvider.GetSubClassTypes<DiagnosticAnalyzer>().Where(type => PLUGINS_FOLDER.IsMatch(type.Assembly.Location) == false).Select(AssemblyProvider.GetSourceFilePath).ToList();
        }

        if (pathList.Any() == false) {
            Logger.TraceError($"{nameof(pathList)} is empty. Please review the implementation through inheriting {nameof(DiagnosticAnalyzer)} again.");
            return;
        }

        GenerateCustomAnalyzerDllOnCompilation(dllName, pathList.ToArray());
    }
    
    private static void GenerateCustomAnalyzerDllOnCompilation(string dllName, params string[] sourceFiles) {
        if (string.IsNullOrEmpty(dllName)) {
            dllName = Constants.Analyzer.ANALYZER_PLUGIN_NAME;
        }
    
        var outputPath = $"{Constants.Path.PROJECT_TEMP_FOLDER}/{dllName.AutoSwitchExtension(Constants.Extension.DLL)}";
        var syntaxTreeList = new List<SyntaxTree>();
        foreach (var path in sourceFiles) {
            if (SystemUtil.TryReadAllText(Path.Combine(Constants.Path.PROJECT_PATH, path), out var source)) {
                syntaxTreeList.Add(SyntaxFactory.ParseSyntaxTree(SourceText.From(source)));
            }
        }
        
        var compilation = CSharpCompilation.Create(dllName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)))
            .AddSyntaxTrees(syntaxTreeList.ToArray());

        OnStart(outputPath);
        if (compilation.Emit(outputPath).Success) {
            OnFinish(outputPath, compilation.Assembly.TypeNames.ConvertTo(name => new CompilerMessage {
                type = CompilerMessageType.Info,
                message = name,
            }).ToArray());
        } else { 
            Logger.TraceError($"Compilation Failed || {outputPath}");
        }
    }
    
    private static void GenerateCustomAnalyzerDllOnAssemblyBuilder(params string[] sourceFiles) {
        if (AssemblyProvider.TryGetUnityAssembly(SystemAssembly.GetExecutingAssembly(), out var assembly)) {
            try {
                var outputPath = $"{Constants.Path.PROJECT_TEMP_FOLDER}/{Constants.Analyzer.ANALYZER_NAME}{Constants.Extension.DLL}";
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
    
    private static void OnStart(string assemblyPath) => Logger.TraceLog($"Start {Path.GetFileName(assemblyPath)} {nameof(Assembly)} build", Color.green);

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
    
    private static void AnalyzerBuildPostProcess(string outputPath) {
        var copyPath = Path.Combine(Constants.Path.PLUGINS_FOLDER, Path.GetFileName(outputPath));
        SystemUtil.MoveFile(outputPath, copyPath);
        File.Delete(outputPath.AutoSwitchExtension(Constants.Extension.PDB));
        AssetDatabase.Refresh();

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