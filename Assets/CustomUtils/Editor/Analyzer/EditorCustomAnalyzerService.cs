using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using UniRx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class EditorCustomAnalyzerService : EditorService {

    private static EditorCustomAnalyzerService _window;
    private static EditorCustomAnalyzerService Window => _window == null ? _window = GetWindow<EditorCustomAnalyzerService>("CustomAnalyzerService") : _window;
    
    private static AnalyzerImplementTreeView _implementAnalyzerTreeView;
    private static AnalyzerImplementTreeView _activateAnalyzerTreeView;
    
    private static Dictionary<Type, AnalyzerImplementInfo> _implementInfoDic = new();
    private static Dictionary<Type, AnalyzerImplementInfo> _activateInfoDic = new();

    private Vector2 _implementInfoScrollViewPosition;
    private Vector2 _activateInfoScrollViewPosition;
    private Vector2 _windowScrollViewPosition;
    
    private static bool _isRefreshing = false;
    private static float _progress = 0;
    
    private static readonly Regex ASSEMBLY_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.SCRIPT_ASSEMBLIES));
    private static readonly Regex PLUGINS_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    protected override void OnEditorOpenInitialize() => CacheRefresh();

    [MenuItem("Analyzer/Analyzer View Service")]
    private static void OpenWindow() {
        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(CacheRefreshCoroutine());
        Window.Show();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorCustomAnalyzerService>()) {
            MainThreadDispatcher.StartEndOfFrameMicroCoroutine(CacheRefreshCoroutine());
        }
    }

    private static IEnumerator CacheRefreshCoroutine() {
        _isRefreshing = true;
        _progress = 0;
        
        yield return null;
        
        var analyzerList = ReflectionProvider.GetSubClassTypes<DiagnosticAnalyzer>().ToList();
        var progressTick = 1.0f / analyzerList.Count;
        foreach (var type in analyzerList) {
            var info = AnalyzerImplementInfo.Create(type);
            if (ASSEMBLY_REGEX.IsMatch(type.Assembly.Location)) {
                _implementInfoDic.AutoAdd(type, info);
            } else if (PLUGINS_REGEX.IsMatch(type.Assembly.Location)) {
                _activateInfoDic.AutoAdd(type, info);
            }

            _progress += progressTick;
            yield return null;
        }
        
        RefreshTreeView(ref _implementAnalyzerTreeView, _implementInfoDic);
        RefreshTreeView(ref _activateAnalyzerTreeView, _activateInfoDic);

        _progress = 1f;
        _isRefreshing = false;
        
        yield return null;
    }

    private Vector2 _scrollViewPosition_1;
    private Vector2 _scrollViewPosition_2;

    private void OnGUI() {
        if (_isRefreshing) {
            var rect = EditorGUILayout.GetControlRect(false, 40f);
            EditorGUI.ProgressBar(rect, _progress, ((int)(_progress * 100)).ToString());
            return;
        }
        
        if (GUILayout.Button("Test Refresh")) {
            MainThreadDispatcher.StartEndOfFrameMicroCoroutine(CacheRefreshCoroutine());
        }
        
        if (_implementAnalyzerTreeView != null && _activateAnalyzerTreeView != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            using (new GUILayout.HorizontalScope()) {
                using (new GUILayout.VerticalScope()) {
                    EditorGUILayout.LabelField("현재 구현 된 Analyzer", Constants.Editor.BOLD_LABEL);
                    EditorGUILayout.Space(3f);
                    
                    _implementAnalyzerTreeView.Draw();
                }
                
                EditorCommon.DrawVerticalSeparator();
                
                using (new GUILayout.VerticalScope()) {
                    EditorGUILayout.LabelField("현재 dll로 적용 된 Analyzer", Constants.Editor.BOLD_LABEL);
                    EditorGUILayout.Space(3f);
                    
                    _activateAnalyzerTreeView.Draw();
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Custom Analyzer DLL 빌드", GUILayout.Height(60f))) {
                EditorCommon.OpenCheckDialogue("경고", "Analyzer dll 파일을 빌드합니다.\n" +
                                                     "환경에 따라 많은 시간이 소요될 수 있습니다.\n" +
                                                     $"빌드가 완료된 후 dll 파일은 자동적으로 {Constants.Path.PLUGINS_FOLDER}로 복사됩니다.", ok: () => {
                    // TODO. 옵션에 따라 파라메터 처리 수정
                    // Temp Method Call
                    AnalyzerGenerator.GenerateCustomAnalyzerDll(null);
                });
            }
        }
    }
    
    private static void RefreshTreeView(ref AnalyzerImplementTreeView treeView, Dictionary<Type, AnalyzerImplementInfo> dictionary) {
        treeView ??= new AnalyzerImplementTreeView();
        
        treeView.Clear();
        foreach (var info in dictionary.Values) {
            treeView.Add(info);
        }
        
        treeView.Reload();
    }
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

internal class AnalyzerImplementTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("명칭", 250f),
    };

    public AnalyzerImplementTreeView() : base(new MultiColumnHeader(new MultiColumnHeaderState(COLUMNS))) { }
    
    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
        EditorCommon.DrawSeparator();
        var index = GetSelection().FirstOrDefault();
        if (index >= 0 && itemList.Count > index && itemList[index] is AnalyzerImplementTreeViewItem item) {
            EditorGUI.TextArea(EditorGUILayout.GetControlRect(GUILayout.MinHeight(100f), GUILayout.MaxHeight(150f)), item.info.description, Constants.Editor.CLIPPING_TEXT_AREA);
        }
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AnalyzerImplementTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Editor.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.info.name);
        }
    }

    public void Clear() => itemList.Clear();
    public void Add(AnalyzerImplementInfo info) => itemList.Add(new AnalyzerImplementTreeViewItem(itemList.Count, info));

    protected override void OnSortingChanged(MultiColumnHeader header) {
        var rows = GetRows();
        if (rows.Count <= 1 || header.sortedColumnIndex == -1) {
            return;
        }
            
        var sortedColumns = header.state.sortedColumns;
        if (sortedColumns.Length <= 0) {
            return;
        }

        if (rootItem.children.TryCast<AnalyzerImplementTreeViewItem>(out var enumerable)) {
            var isAscending = multiColumnHeader.IsSortedAscending(sortedColumns.First());
            switch (EnumUtil.ConvertInt<SORT_TYPE>(sortedColumns.First())) {
                case SORT_TYPE.NO:
                    enumerable = enumerable.OrderBy(x => x.id, isAscending);
                    break;
                case SORT_TYPE.NAME:
                    enumerable = enumerable.OrderBy(x => x.info.name, isAscending);
                    break;
            }
            
            rootItem.children = enumerable.CastList<TreeViewItem>();
            
            rows.Clear();
            rootItem.children.ForEach(x => rows.Add(x));
            
            Repaint();
        }
    }

    private sealed class AnalyzerImplementTreeViewItem : TreeViewItem {

        public AnalyzerImplementInfo info;
        
        public AnalyzerImplementTreeViewItem(int id, AnalyzerImplementInfo info) {
            this.id = id;
            this.info = info;
            depth = 0;
        }
    }
    
    private enum SORT_TYPE {
        NO,
        NAME,
    }
}
