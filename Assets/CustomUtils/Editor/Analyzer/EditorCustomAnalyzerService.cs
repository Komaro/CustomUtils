using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using UniRx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class EditorCustomAnalyzerService : EditorService {

    private static EditorCustomAnalyzerService _window;
    private static EditorCustomAnalyzerService Window => _window == null ? _window = GetWindow<EditorCustomAnalyzerService>("CustomAnalyzerService") : _window;

    private string _dllName;
    
    private static AnalyzerImplementTreeView _implementAnalyzerTreeView;
    private static AnalyzerImplementTreeView _activateAnalyzerTreeView;
    
    private static Dictionary<Type, AnalyzerImplementInfo> _implementInfoDic = new();
    private static Dictionary<Type, AnalyzerImplementInfo> _activateInfoDic = new();
    private static HashSet<Assembly> _activateAssemblySet = new();

    private Vector2 _implementInfoScrollViewPosition;
    private Vector2 _activateInfoScrollViewPosition;
    private Vector2 _windowScrollViewPosition;

    private bool _activateAssemblySetFold;
    
    private static bool _isRefreshing;
    private static float _progress;
    
    private static readonly Regex ASSEMBLY_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.SCRIPT_ASSEMBLIES));
    private static readonly Regex PLUGINS_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    protected override void OnEditorOpenInitialize() => CacheRefresh();

    [MenuItem("Service/Analyzer/Analyzer View Service")]
    private static void OpenWindow() {
        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(CacheRefreshCoroutine());
        Window.Show();
        Window.Focus();
    }

    [MenuItem("Service/Analyzer/Analyzer View Service Close")]
    private static void CloseWindow() {
        Window.Close();
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
        
        var analyzerList = ReflectionProvider.GetSubClassTypes<DiagnosticAnalyzer>().Where(type => type.IsAbstract == false).ToList();
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

        _implementAnalyzerTreeView ??= new AnalyzerImplementTreeView();
        _activateAnalyzerTreeView ??= new AnalyzerActivateTreeView();

        RefreshTreeView(ref _implementAnalyzerTreeView, _implementInfoDic);
        RefreshTreeView(ref _activateAnalyzerTreeView, _activateInfoDic);

        _progress = 1f;
        _isRefreshing = false;

        if (_activateInfoDic.Any()) {
            _activateAssemblySet.Clear();
            _activateAssemblySet = _activateInfoDic.Values.Select(info => info.type.Assembly).ToHashSetWithDistinct();
        }
        
        yield return null;
    }

    private void OnGUI() {
        if (_isRefreshing) {
            var rect = EditorGUILayout.GetControlRect(false, 40f);
            EditorGUI.ProgressBar(rect, _progress, ((int)(_progress * 100)).ToString());
            return;
        }
        
        using (new GUILayout.HorizontalScope()) {
            if (EditorCommon.DrawLabelButton("Custom Analyzer", Constants.Draw.REFRESH_ICON, Constants.Draw.AREA_TITLE_STYLE)) {
                CacheRefresh();
            }

            if (GUILayout.Button("일괄 선택")) {
                _implementInfoDic.Values.ForEach(info => info.isCheck = true);
                _implementAnalyzerTreeView.Repaint();
            }

            if (GUILayout.Button("일괄 선택 해제")) {
                _implementInfoDic.Values.ForEach(info => info.isCheck = false);
                _implementAnalyzerTreeView.Repaint();
            }
            
            GUILayout.FlexibleSpace();
        }
        
        if (_implementAnalyzerTreeView != null && _activateAnalyzerTreeView != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            using (new GUILayout.HorizontalScope(Constants.Draw.BOX)) {
                using (new GUILayout.VerticalScope()) {
                    EditorGUILayout.LabelField("현재 구현 된 Analyzer", Constants.Draw.BOLD_LABEL);
                    EditorGUILayout.Space(3f);
                    
                    _implementAnalyzerTreeView.Draw();
                }
                
                EditorCommon.DrawVerticalSeparator();
                
                using (new GUILayout.VerticalScope()) {
                    // TODO. 표기 개선 필요
                    if (_activateAssemblySet.Any()) {
                        _activateAssemblySetFold = EditorGUILayout.BeginFoldoutHeaderGroup(_activateAssemblySetFold, string.Empty);
                        if (_activateAssemblySetFold) {
                            foreach (var assembly in _activateAssemblySet) {
                                var time = File.GetLastWriteTime(assembly.Location);
                                EditorGUILayout.LabelField($"{assembly.GetName().Name} || {time}");
                            }
                        }
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                    
                    EditorGUILayout.LabelField("현재 dll로 적용 된 Analyzer", Constants.Draw.BOLD_LABEL);
                    EditorGUILayout.Space(3f);
                    
                    _activateAnalyzerTreeView.Draw();
                }
            }
            
            EditorGUILayout.EndScrollView();

            if (_implementInfoDic?.IsEmpty() ?? false) {
                EditorGUILayout.HelpBox($"구현된 {nameof(DiagnosticAnalyzer)}가 존재하지 않거나 {nameof(OnGUI)} 도중 문제가 발생하였을 수 있습니다.", MessageType.Warning);
            } else {
                EditorCommon.DrawLabelTextFieldWithRefresh("DLL 파일 명칭", ref _dllName, () => _dllName = _dllName = Constants.Analyzer.DEFAULT_ANALYZER_PLUGIN);
                if (string.IsNullOrEmpty(_dllName)) {
                    EditorGUILayout.HelpBox("빌드할 DLL 파일의 명칭을 지정해야 합니다. 확장자는 생략할 수 있습니다.", MessageType.Error);
                } else {
                    if (GUILayout.Button("Custom Analyzer DLL 빌드", GUILayout.Height(60f))) {
                        EditorCommon.OpenCheckDialogue("경고", "Analyzer dll 파일을 빌드합니다.\n" +
                                                             "환경에 따라 많은 시간이 소요될 수 있습니다.\n" +
                                                             $"빌드가 완료된 후 {_dllName} 파일은 자동적으로 {Constants.Path.PLUGINS_FOLDER}로 복사됩니다.", ok: () => {
                            
                            AnalyzerGenerator.GenerateCustomAnalyzerDll(_dllName, _implementInfoDic.Values.Where(info => info.isCheck).Select(info => info.type));
                        });
                    }
                }
            }
        }
    }
    
    private static void RefreshTreeView(ref AnalyzerImplementTreeView treeView, Dictionary<Type, AnalyzerImplementInfo> dictionary) {
        treeView.Clear();
        foreach (var info in dictionary.Values) {
            treeView.Add(info);
        }
        
        treeView.Reload();
    }
}

internal record AnalyzerImplementInfo {

    public Type type;
    public string name;
    public string description;
    public bool isOpen;
    public bool isCheck;

    public static AnalyzerImplementInfo Create(Type analyzerType) {
        var info = new AnalyzerImplementInfo {
            type = analyzerType,
            name = analyzerType.Name,
            description = analyzerType.TryGetCustomAttribute<DescriptionAttribute>(out var descriptionAttribute) ? descriptionAttribute.Description : string.Empty
        };
        
        return info;
    }
}

#region [TreeView]

internal class AnalyzerActivateTreeView : AnalyzerImplementTreeView {

    protected new static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("어셈블리",70f),
        CreateColumn("명칭", 350f),
    };
    
    public AnalyzerActivateTreeView() : base(COLUMNS) { }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AnalyzerImplementTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.info.type.Assembly.GetName().Name, Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(2), item.info.name);
        }
    }

    // TODO. 현재 선택된 데이터의 Assembly 정보다 함께 출력
    public override void Draw() {
        base.Draw();
    }
}

internal class AnalyzerImplementTreeView : EditorServiceTreeView {

    protected static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("선택", 35f, 35f),
        CreateColumn("명칭", 350f),
    };
    
    public AnalyzerImplementTreeView() : base(COLUMNS) { }
    public AnalyzerImplementTreeView(MultiColumnHeaderState.Column[] columns) : base(columns) { }

    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100f)));
        EditorCommon.DrawSeparator();
        var index = GetSelection().FirstOrDefault();
        if (index >= 0 && itemList.Count > index && itemList[index] is AnalyzerImplementTreeViewItem item) {
            EditorGUILayout.TextField(item.info.name, Constants.Draw.BOLD_LABEL);
            EditorGUI.TextArea(EditorGUILayout.GetControlRect(GUILayout.MinHeight(50f), GUILayout.MaxHeight(80f)), item.info.description, Constants.Draw.CLIPPING_TEXT_AREA);
        }
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AnalyzerImplementTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorCommon.DrawFitToggle(args.GetCellRect(1), ref item.info.isCheck);
            EditorGUI.LabelField(args.GetCellRect(2), item.info.name);
        }
    }

    public void Clear() => itemList.Clear();
    public void Add(AnalyzerImplementInfo info) => itemList.Add(new AnalyzerImplementTreeViewItem(itemList.Count, info));

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        if (rootItem.children.TryCast<AnalyzerImplementTreeViewItem>(out var enumerable)) {
            enumerable = EnumUtil.ConvertInt<SORT_TYPE>(index) switch {
                SORT_TYPE.NO => enumerable.OrderBy(x => x.id, isAscending),
                SORT_TYPE.NAME => enumerable.OrderBy(x => x.info.name, isAscending),
                _ => enumerable
            };

            return enumerable;
        }

        return Enumerable.Empty<TreeViewItem>();
    }

    protected sealed class AnalyzerImplementTreeViewItem : TreeViewItem {

        public AnalyzerImplementInfo info;
        
        public AnalyzerImplementTreeViewItem(int id, AnalyzerImplementInfo info) {
            this.id = id;
            this.info = info;
            depth = 0;
        }
    }
    
    protected enum SORT_TYPE {
        NO,
        NAME,
    }
}

#endregion
