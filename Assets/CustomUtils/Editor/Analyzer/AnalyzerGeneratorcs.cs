using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class AnalyzerGenerator {

    private static readonly Regex PLUGINS_FOLDER = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    // [MenuItem("Analyzer/Test Call")]
    private static void TestCall() {
        GenerateCustomAnalyzerDll();
    }

    public static void GenerateCustomAnalyzerDll(params string[] sourceFiles) {
        if (sourceFiles is not { Length: > 0 }) {
            Logger.TraceLog($"{nameof(sourceFiles)} is empty. Building all Analyzers that inherit from {nameof(DiagnosticAnalyzer)}.", Color.yellow);
            var sourceFileDic = AssemblyProvider.GetUnityAssembly().Values.SelectMany(x => x.sourceFiles).ToDictionaryWithDistinct(Path.GetFileNameWithoutExtension, path => path);
            var implementAnalyzerPathDic = ReflectionProvider.GetSubClassTypes<DiagnosticAnalyzer>().Where(type => PLUGINS_FOLDER.IsMatch(type.Assembly.Location) == false).Select(type => type.Name).ToDictionary(name => name, name => sourceFileDic.TryGetValue(name, out var path) ? path : string.Empty);
            sourceFiles = implementAnalyzerPathDic.Values.Where(path => string.IsNullOrEmpty(path) == false).ToArray();
        }
        
        if (AssemblyProvider.TryGetUnityAssembly(SystemAssembly.GetExecutingAssembly(), out var assembly)) {
            try {
                var outputPath = $"{Constants.Path.PROJECT_TEMP_FOLDER}/{Constants.Analyzer.ANALYZER_PLUGIN_NAME}{Constants.Extension.DLL}";
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
    
    private static void AnalyzerPostProcess(string outputPath) {
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
        AnalyzerPostProcess(outputPath);
    }
}