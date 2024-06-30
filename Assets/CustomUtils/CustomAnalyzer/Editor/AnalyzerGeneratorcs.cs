using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CustomUtils.Common;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using SystemAssembly = System.Reflection.Assembly;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public static class AnalyzerGenerator {

    private const string ANALYZER_NAME = "CustomUtilsAnalyzer";
    private const string ROSLYN_ANALYZER_LABEL = "RoslynAnalyzer";
    private static readonly Regex EDITOR_REGEX = new(string.Format(Constants.Text.FOLDER_CONTAINS_FORMAT, "Editor"));
    
    [MenuItem("Analyzer/Build Custom Analyzer")]
    private static void BuildAnalyzer() {
        if (AssemblyProvider.TryGetUnityAssembly(SystemAssembly.GetExecutingAssembly(), out var assembly)) {
            try {
                var outputPath = $"{Constants.Path.PROJECT_TEMP_FOLDER}/{ANALYZER_NAME}{Constants.Extension.DLL}";
                var builder = new AssemblyBuilder(outputPath, assembly.sourceFiles.Where(x => EDITOR_REGEX.IsMatch(x) == false).ToArray());
                builder.buildStarted += OnStart;
                builder.buildFinished += OnFinish;
                builder.Build();
            } catch (Exception ex) {
                Logger.TraceError(ex);
                throw;
            }
        }
    }

    [MenuItem("Analyzer/Dll Move and Clear")]
    private static void MenuCopyWithClear() => AnalyzerPostProcess($"{Constants.Path.PROJECT_TEMP_FOLDER}/{ANALYZER_NAME}{Constants.Extension.DLL}");

    private static void AnalyzerPostProcess(string outputPath) {
        var copyPath = Path.Combine(Constants.Path.PLUGINS_FOLDER, Path.GetFileName(outputPath));
        SystemUtil.MoveFile(outputPath, copyPath);
        File.Delete(Path.ChangeExtension(outputPath, Constants.Extension.PDB));
        AssetDatabase.Refresh();

        if (AssetDatabaseUtil.TryLoad(AssetDatabaseUtil.GetCollectionPath(copyPath), out var dllAsset)) {
            var labels = AssetDatabase.GetLabels(dllAsset);
            if (labels.Contains(ROSLYN_ANALYZER_LABEL) == false) {
                var newLabels = labels.Union(new[] { ROSLYN_ANALYZER_LABEL }).ToArray();
                AssetDatabase.SetLabels(dllAsset, newLabels);
            }
        }
    }

    private static void OnStart(string assemblyPath) {
        Logger.TraceLog($"Start {Path.GetFileName(assemblyPath)} {nameof(Assembly)} build", Color.green);
    }

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
                    
        AnalyzerPostProcess(outputPath);
    }
}
