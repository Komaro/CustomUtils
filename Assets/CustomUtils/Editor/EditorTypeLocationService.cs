using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Assembly = UnityEditor.Compilation.Assembly;
using Color = System.Drawing.Color;

public partial class EditorTypeLocationService : EditorService<EditorTypeLocationService> {

    private static bool _isActiveAnalyze;

    private SerializedAssemblyArray _ignoreAssemblyAssets;

    private static readonly ConcurrentDictionary<Type, (string path, int line)> _typeLocationDic = new();
    
    private const string DEFAULT_PATH = "Service/TypeLocation";
    private const string ACTIVE_TOGGLE_PATH = DEFAULT_PATH + "/Active";
    private const string WINDOW_PATH = DEFAULT_PATH + "/Type Location Service";

    [InitializeOnLoadMethod]
    private static void Init() {
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        _isActiveAnalyze = EditorPrefsUtil.GetBool(ACTIVE_TOGGLE_PATH);
    }

    private static void OnAfterAssemblyReload() {
        if (_isActiveAnalyze) {
            AnalyzeTypeLocation();
        }
    }

    [MenuItem(WINDOW_PATH)]
    private static void OpenWindow() => Window.Open();
    
    protected override void Refresh() => _ignoreAssemblyAssets ??= CreateInstance<SerializedAssemblyArray>();

    private void OnGUI() {
        EditorGUILayout.Space(10);
        
        _isActiveAnalyze = EditorGUILayout.ToggleLeft("자동 타입 분석 활성화", _isActiveAnalyze);
        
        EditorCommon.DrawSeparator();

        if (_isActiveAnalyze) {
            if (GUILayout.Button("타입 재분석", GUILayout.Height(50f))) {
                AnalyzeTypeLocation(true);
            }
            
            EditorGUI.BeginDisabledGroup(IsRunning);
            
            if (_ignoreAssemblyAssets.Value != null) {
                EditorGUILayout.LabelField("어셈블리 제외 리스트");
                _ignoreAssemblyAssets.Draw();
            }
            
            if (IsRunning) {
                EditorGUILayout.HelpBox("타입을 다시 분석하는 중입니다...", MessageType.Warning);
            } else {
                // TODO. Show Type Location Statistics
            }
            
            EditorGUI.EndDisabledGroup();
        }
    }

    public static bool TryGetTypeLocation(Type type, out (string path, int line) location) => (location = GetTypeLocation(type)) != default; 
    public static (string path, int line) GetTypeLocation(Type type) => _typeLocationDic.TryGetValue(type, out var location) ? location : default;

    public static bool ContainsTypeLocation(Type type) => _typeLocationDic.ContainsKey(type);

    private class SerializedAssemblyArray : SerializedArray<AssemblyDefinitionAsset> {

        public const string IGNORE_ASSEMBLIES_KEY = "IGNORE_ASSEMBLIES";

        protected override void OnRemove(ReorderableList list) {
            base.OnRemove(list);
            Save();
        }

        protected override void OnEnable() {
            base.OnEnable();
            Load();
        }

        public void Save() => EditorPrefsUtil.Set(IGNORE_ASSEMBLIES_KEY, Value.Select(asset => asset.name));

        // TODO. 옵션에 따라 AssetDatabase에서 찾을 수 없는 경우 로드할 수 없는 문제 발생. 일반적으로 목정 원래 목적에 맞게 동작하나 예외에 대해선 처리되지 않아 오작동으로 보일 수 있음
        public void Load() {
            if (EditorPrefsUtil.TryGet(IGNORE_ASSEMBLIES_KEY, out string[] ignoreAssemblies)) {
                Value = AssetDatabaseUtil.FindAssets<AssemblyDefinitionAsset>(FilterUtil.CreateFilter(AreaFilter.all)).Where(asset => ignoreAssemblies.Contains(asset.name)).ToArray();
            }
            
            serializedObj.Update();
        }

        protected override void OnChanged(AssemblyDefinitionAsset value, int index) => Save();
    }
}


#if UNITY_2022_3_OR_NEWER

// Thread
public partial class EditorTypeLocationService {

    private static int _taskId;
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
            await Task.Run(() => assemblyDic.AsParallel().WithCancellation(_tokenSource.Token).ForAll(pair => AnalyzeTypeLocation(pair.Key, pair.Value, _tokenSource.Token)), _tokenSource.Token);
            Progress.Remove(_taskId);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            IsRunning = false;
        }
    }

    private static void AnalyzeTypeLocation(Assembly assembly, string[] sourceFiles, CancellationToken token) {
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
                    if (semanticModel.TryGetDeclaredSymbol(syntax, out var symbol) && systemAssembly.TryGetType(symbol.ToDisplayString(), out var type)) {
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
    
#else
    // Coroutine
        
    public static partial class EditorTypeLocationService {
        
    }
#endif
}