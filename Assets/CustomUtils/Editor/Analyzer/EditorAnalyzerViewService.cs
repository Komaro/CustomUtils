using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// TODO. TreeView로 전환
public class EditorAnalyzerViewService : EditorService {

    private static EditorAnalyzerViewService _window;
    private static EditorAnalyzerViewService Window => _window ??= GetWindow<EditorAnalyzerViewService>("AnalyzerViewService");

    private AnalyzerImplementTreeView _treeView;
    
    private static Dictionary<Type, AnalyzerImplementInfo> _implementInfoDic = new();
    private static Dictionary<Type, AnalyzerImplementInfo> _activateInfoDic = new();

    private Vector2 _implementInfoScrollViewPosition;
    private Vector2 _activateInfoScrollViewPosition;

    private static readonly Regex ASSEMBLY_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.SCRIPT_ASSEMBLIES));
    private static readonly Regex PLUGINS_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    protected override void OnEditorOpenInitialize() => CacheRefresh();

    [MenuItem("Analyzer/Analyzer View Service")]
    private static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false).ToList();
        var types = assemblies.Where(assembly => ASSEMBLY_REGEX.IsMatch(assembly.Location))
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsSubclassOf(typeof(DiagnosticAnalyzer)));
        
        _implementInfoDic.Clear();
        foreach (var type in types) {
            _implementInfoDic.AutoAdd(type, AnalyzerImplementInfo.Create(type));
        }
        
        if (AssetDatabaseUtil.TryGetLabels(Constants.Analyzer.ANALYZER_PLUGIN_PATH, out var labels) && labels.Length > 0) {
            types = assemblies.Where(assembly => PLUGINS_REGEX.IsMatch(assembly.Location))
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.IsSubclassOf(typeof(DiagnosticAnalyzer)) && type.IsDefined<DescriptionAttribute>());
            
            foreach (var type in types) {
                _activateInfoDic.AutoAdd(type, AnalyzerImplementInfo.Create(type));
            }
        }
    }

    private void OnGUI() {
        // TODO. Analyzer 빌드 추가
        
        using (new EditorGUILayout.HorizontalScope()) {
            using (new EditorGUILayout.VerticalScope()) {
                EditorGUILayout.LabelField("현재 구현된 Analyzer", Constants.Editor.BOLD_LABEL);
                _implementInfoScrollViewPosition = EditorGUILayout.BeginScrollView(_implementInfoScrollViewPosition, false, false);
                foreach (var info in _implementInfoDic.Values) {
                    using (new EditorGUILayout.VerticalScope()) {
                        EditorGUILayout.LabelField(info.name);
                        info.isOpen = EditorGUILayout.BeginFoldoutHeaderGroup(info.isOpen, "Description");
                        if (info.isOpen) {
                            EditorGUILayout.TextArea(info.description, GUILayout.MinHeight(50f), GUILayout.MaxHeight(100f));
                        }
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUILayout.VerticalScope()) {
                EditorGUILayout.LabelField("현재 Plugin을 통해 적용된 Analyzer", Constants.Editor.BOLD_LABEL);
                _activateInfoScrollViewPosition = EditorGUILayout.BeginScrollView(_activateInfoScrollViewPosition, false, false);
                foreach (var info in _activateInfoDic.Values) {
                    EditorGUILayout.LabelField(info.name);
                    if (info.hasDescription) {
                        info.isOpen = EditorGUILayout.BeginFoldoutHeaderGroup(info.isOpen, "Description");
                        if (info.isOpen) {
                            EditorGUILayout.TextArea(info.description, GUILayout.MinHeight(50f), GUILayout.MaxHeight(100f));
                        }
                        
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}

internal class AnalyzerImplementTreeView : TreeView {

    public AnalyzerImplementTreeView(TreeViewState state) : base(state) {
    }

    public AnalyzerImplementTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) {
    }

    protected override TreeViewItem BuildRoot() => throw new NotImplementedException();
}

internal record AnalyzerImplementInfo {

    public string name;
    public string description;
    public bool hasDescription;
    public bool isOpen;

    public static AnalyzerImplementInfo Create(Type analyzerType) {
        var info = new AnalyzerImplementInfo {
            name = analyzerType.Name,
            description = analyzerType.TryGetCustomAttribute<DescriptionAttribute>(out var descriptionAttribute) ? descriptionAttribute.Description : string.Empty
        };
        
        info.hasDescription = string.IsNullOrEmpty(info.description) == false;
        return info;
    }
}