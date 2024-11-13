using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class EditorCustomAnalyzerService : EditorService {

    private static EditorCustomAnalyzerService _window;
    private static EditorCustomAnalyzerService Window => _window == null ? _window = GetWindow<EditorCustomAnalyzerService>("CustomAnalyzerService") : _window;

    private static string _dllName;
    
    private static AnalyzerImplementTreeView _implementAnalyzerTreeView;
    
    private static ActivateAssemblyTreeView _activateAssemblyTreeView;
    private static AnalyzerImplementTreeView _activateAnalyzerTreeView;
    
    private static readonly Dictionary<Type, AnalyzerImplementInfo> _implementInfoDic = new();
    
    private static HashSet<Assembly> _activateAssemblySet = new();
    private static readonly Dictionary<Type, AnalyzerImplementInfo> _activateInfoDic = new();

    private Vector2 _windowScrollViewPosition;
    
    private readonly AnimBool _activateAssemblyTreeViewFold = new();
    private Vector2 _activateAssemblyScrollViewPosition;
    
    private static bool _isRefreshing;
    private static float _progress;
    
    private static readonly Regex ASSEMBLY_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.SCRIPT_ASSEMBLIES));
    private static readonly Regex PLUGINS_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, Constants.Folder.PLUGINS));

    protected override void OnEditorOpenInitialize() => CacheRefresh();

    [MenuItem("Service/Analyzer/Analyzer View Service")]
    private static void OpenWindow() {
        EditorCoroutineUtility.StartCoroutine(CacheRefreshCoroutine(), Window);
        Window.Show();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorCustomAnalyzerService>()) {
            EditorCoroutineUtility.StartCoroutine(CacheRefreshCoroutine(), Window);
        }
    }

    private static IEnumerator CacheRefreshCoroutine() {
        if (string.IsNullOrEmpty(_dllName)) {
            _dllName = Constants.Analyzer.ANALYZER_PLUGIN_NAME;
        }
        
        _isRefreshing = true;
        _progress = 0;
        
        yield return null;
        
        var analyzerList = ReflectionProvider.GetSubClassTypes<DiagnosticAnalyzer>().Where(type => type.IsAbstract == false && type.IsDefined<ObsoleteAttribute>() == false).ToList();
        var progressTick = 1.0f / analyzerList.Count;
        foreach (var type in analyzerList) {
            if (ASSEMBLY_REGEX.IsMatch(type.Assembly.Location)) {
                _implementInfoDic.AutoAdd(type, new AnalyzerImplementInfo(type));
            } else if (PLUGINS_REGEX.IsMatch(type.Assembly.Location) && AssetDatabaseUtil.ContainsLabel(type.Assembly.Location, Constants.Analyzer.ROSLYN_ANALYZER_LABEL)) {
                _activateInfoDic.AutoAdd(type, new AnalyzerActivateInfo(type));
            }

            _progress += progressTick;
            yield return null;
        }

        _implementAnalyzerTreeView ??= new AnalyzerImplementTreeView();
        _implementAnalyzerTreeView.ClearReload(_implementInfoDic.Values);
        
        _activateAnalyzerTreeView ??= new AnalyzerActivateTreeView();
        _activateAnalyzerTreeView.ClearReload(_activateInfoDic.Values);

        _progress = 1f;
        _isRefreshing = false;

        if (_activateInfoDic.Any()) {
            _activateAssemblySet.Clear();
            _activateAssemblySet = _activateInfoDic.Values.Select(info => info.type.Assembly).ToHashSetWithDistinct();

            _activateAssemblyTreeView ??= new ActivateAssemblyTreeView();
            _activateAssemblyTreeView.ClearReload(_activateAssemblySet);
        }
        
        yield return null;
    }
    
    private void OnEnable() => _activateAssemblyTreeViewFold?.valueChanged.AddListener(Repaint);
    private void OnDisable() => _activateAssemblyTreeViewFold?.valueChanged.RemoveListener(Repaint);

    private void OnGUI() {
        if (_isRefreshing) {
            var rect = EditorGUILayout.GetControlRect(false, 40f);
            EditorGUI.ProgressBar(rect, _progress, ((int)(_progress * 100)).ToString());
            return;
        }
        
        if (EditorCommon.DrawLabelButton("커스텀 분석기(Analyzer)", Constants.Draw.REFRESH_ICON, Constants.Draw.AREA_TITLE_STYLE)) {
            CacheRefresh();
        }
        
        if (_implementAnalyzerTreeView != null && _activateAnalyzerTreeView != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            using (new GUILayout.HorizontalScope(Constants.Draw.BOX)) {
                using (new GUILayout.VerticalScope()) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorCommon.DrawFitLabel("현재 구현 된 분석기(Analyzer)", Constants.Draw.AREA_TITLE_STYLE);
                        if (EditorCommon.DrawFitButton("일괄 선택")) {
                            _implementInfoDic.Values.ForEach(info => info.isCheck = true);
                            _implementAnalyzerTreeView.Repaint();
                        }
                        
                        if (EditorCommon.DrawFitButton("일괄 선택 해제")){
                            _implementInfoDic.Values.ForEach(info => info.isCheck = false);
                            _implementAnalyzerTreeView.Repaint();
                        }
                    }
                    
                    EditorGUILayout.Space(3f);
                    
                    _implementAnalyzerTreeView.Draw();
                }
                
                EditorCommon.DrawVerticalSeparator();
                
                using (new GUILayout.VerticalScope()) {
                    _activateAssemblyScrollViewPosition = EditorGUILayout.BeginScrollView(_activateAssemblyScrollViewPosition, false, false, GUILayout.ExpandHeight(true));
                    if (_activateAssemblySet.Any()) {
                        _activateAssemblyTreeViewFold.target = EditorGUILayout.BeginFoldoutHeaderGroup(_activateAssemblyTreeViewFold.target, "적용 중인 어셈블리(Assembly) 목록", Constants.Draw.TITLE_FOLD_OUT_HEADER_STYLE);
                        if (EditorGUILayout.BeginFadeGroup(_activateAssemblyTreeViewFold.faded)) {
                            _activateAssemblyTreeView.Draw();
                            EditorCommon.DrawSeparator(1f, 3f);
                        }
                        
                        EditorGUILayout.EndFadeGroup();
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }

                    EditorGUILayout.LabelField("적용 중인 분석기(Analyzer)", Constants.Draw.AREA_TITLE_STYLE);
                    EditorGUILayout.Space(3f);
                    
                    _activateAnalyzerTreeView.Draw();
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                if (_implementInfoDic?.IsEmpty() ?? false) {
                    EditorGUILayout.HelpBox($"구현된 {nameof(DiagnosticAnalyzer)}가 존재하지 않거나 {nameof(OnGUI)} 도중 문제가 발생하였을 수 있습니다.", MessageType.Warning);
                } else {
                    EditorCommon.DrawLabelTextFieldWithRefresh("DLL(Assembly) 파일 명칭", ref _dllName, () => _dllName = _dllName = Constants.Analyzer.ANALYZER_PLUGIN_NAME);
                    if (string.IsNullOrEmpty(_dllName)) {
                        EditorGUILayout.HelpBox("빌드할 DLL 파일의 명칭을 지정해야 합니다. 확장자는 생략할 수 있습니다.", MessageType.Error);
                    } else {
                        EditorGUILayout.HelpBox("DLL파일의 명칭과 어셈블리의 명칭은 동일하게 적용됩니다. 여러개의 DLL 파일로 나누는 경우 명칭을 다르게 지정하여야 합니다.", MessageType.Info);
                        using (new EditorGUI.DisabledGroupScope(EditorApplication.isCompiling)) {
                            if (GUILayout.Button("Custom Analyzer DLL 빌드", GUILayout.Height(30f))) {
                                EditorCommon.ShowCheckDialogue("경고", "Analyzer dll 파일을 빌드합니다.\n" +
                                                                     "환경에 따라 많은 시간이 소요될 수 있습니다.\n" +
                                                                     $"빌드가 완료된 후 {_dllName} 파일은 자동적으로 {Constants.Path.PLUGINS_FULL_PATH}로 복사됩니다.", ok: () => {
                                    
                                    AnalyzerGenerator.GenerateCustomAnalyzerDll(_dllName, _implementInfoDic.Values.Where(info => info.isCheck).Select(info => info.type));
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}

internal record AnalyzerActivateInfo : AnalyzerImplementInfo {

    public AnalyzerActivateInfo(Type analyzerType) : base(analyzerType) { }

    public override bool IsMatch(string match) => assemblyName.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0 || base.IsMatch(match);
}

internal record AnalyzerImplementInfo {

    public readonly Type type;
    public readonly string assemblyName;
    public readonly string name;
    public readonly string description;
    public readonly bool isValid; 
    
    public bool isOpen;
    public bool isCheck;

    public AnalyzerImplementInfo(Type analyzerType) {
        type = analyzerType;
        assemblyName = analyzerType.Assembly.GetName().Name;
        name = analyzerType.Name;
        description = analyzerType.TryGetCustomAttribute<DescriptionAttribute>(out var descriptionAttribute) ? descriptionAttribute.Description : string.Empty;
        isValid = analyzerType.IsDefined<DiagnosticAnalyzerAttribute>();
    }

    public virtual bool IsMatch(string match) => name.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
}

#region [TreeView]

internal class ActivateAssemblyTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("명칭", 100f),
        CreateColumn("마지막 수정", 60f),
    };

    public ActivateAssemblyTreeView() : base(COLUMNS) { }
    
    public override void Draw() => OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(50f), GUILayout.MaxHeight(110f))));

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is ActivateAssemblyTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.name, Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(2), item.lastWriteDate.ToString(CultureInfo.CurrentCulture), Constants.Draw.CENTER_LABEL);
        }
    }

    public void ClearReload(IEnumerable<Assembly> enumerable) {
        Clear();
        enumerable.ForEach(Add);
        Reload();
    }

    public void Add(Assembly assembly) => itemList.Add(new ActivateAssemblyTreeViewItem(itemList.Count, assembly));

    private sealed class ActivateAssemblyTreeViewItem : TreeViewItem {

        public readonly string name;
        public readonly DateTime lastWriteDate;
        
        
        public ActivateAssemblyTreeViewItem(int id, Assembly assembly) {
            this.id = id;
            name = assembly.GetName().Name;
            lastWriteDate = File.GetLastWriteTime(assembly.Location);
        }
    }
}

internal class AnalyzerActivateTreeView : AnalyzerImplementTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("어셈블리",70f),
        CreateColumn("명칭", 350f),
        CreateColumn("유효", 35f, 35f),
    };
    
    public AnalyzerActivateTreeView() : base(COLUMNS) { }
    
    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AnalyzerImplementTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.info.assemblyName, Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(2), item.info.name);
            EditorCommon.DrawFitToggle(args.GetCellRect(3), item.info.isValid);
        }
    }

    protected override IEnumerable<TreeViewItem> GetOrderBy(SORT_TYPE type, bool isAscending, IEnumerable<AnalyzerImplementTreeViewItem> enumerable) {
        switch (type) {
            case SORT_TYPE.ASSEMBLY:
                return enumerable.OrderBy(x => x.info.assemblyName, isAscending);
        }

        return base.GetOrderBy(type, isAscending, enumerable);
    }
}

internal class AnalyzerImplementTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("선택", 35f, 35f),
        CreateColumn("명칭", 350f),
        CreateColumn("유효", 35f, 35f),
    };
    
    public AnalyzerImplementTreeView() : base(COLUMNS) { }
    public AnalyzerImplementTreeView(MultiColumnHeaderState.Column[] columns) : base(columns) { }
    
    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100f)));
        EditorCommon.DrawSeparator();
        var index = GetSelection().FirstOrDefault();
        if (index >= 0 && itemList.Count > index && itemList[index] is AnalyzerImplementTreeViewItem item) {
            EditorCommon.DrawLabelTextField("어셈블리", item.info.assemblyName, 60f, Constants.Draw.BOLD_TEXT_FIELD);
            EditorCommon.DrawLabelTextField("명칭", item.info.name, 60f, Constants.Draw.BOLD_TEXT_FIELD);
            EditorGUILayout.Space(2f);
            EditorGUI.TextArea(EditorGUILayout.GetControlRect(GUILayout.MinHeight(50f), GUILayout.MaxHeight(80f)), item.info.description, Constants.Draw.CLIPPING_TEXT_AREA);
        }
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AnalyzerImplementTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorCommon.DrawFitToggle(args.GetCellRect(1), ref item.info.isCheck);
            EditorGUI.LabelField(args.GetCellRect(2), item.info.name);
            EditorCommon.DrawFitToggle(args.GetCellRect(3), item.info.isValid);
        }
    }

    public void ClearReload(IEnumerable<AnalyzerImplementInfo> enumerable) {
        Clear();
        enumerable.ForEach(Add);
        Reload();
    }

    public virtual void Add(AnalyzerImplementInfo info) => itemList.Add(new AnalyzerImplementTreeViewItem(itemList.Count, info));

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        if (rootItem.children.TryCast<AnalyzerImplementTreeViewItem>(out var enumerable) && EnumUtil.TryConvert<SORT_TYPE>(index, out var type)) {
            return GetOrderBy(type, isAscending, enumerable);
        }

        return Enumerable.Empty<TreeViewItem>();
    }

    protected virtual IEnumerable<TreeViewItem> GetOrderBy(SORT_TYPE type, bool isAscending, IEnumerable<AnalyzerImplementTreeViewItem> enumerable) {
        enumerable = type switch {
            SORT_TYPE.NO => enumerable.OrderBy(x => x.id, isAscending),
            SORT_TYPE.SELECTION => enumerable.OrderBy(x => x.info.isCheck, isAscending),
            SORT_TYPE.NAME => enumerable.OrderBy(x => x.info.name, isAscending),
            SORT_TYPE.VALID => enumerable.OrderBy(x => x.info.isValid, isAscending),
            _ => enumerable
        };

        return enumerable;
    }
    
    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => item is AnalyzerImplementTreeViewItem implementItem && implementItem.info.IsMatch(search);

    protected sealed class AnalyzerImplementTreeViewItem : TreeViewItem {

        public AnalyzerImplementInfo info;
        
        public AnalyzerImplementTreeViewItem(int id, AnalyzerImplementInfo info) {
            this.id = id;
            this.info = info;
            depth = 0;
        }
    }
}

internal enum SORT_TYPE {
    NO,
    SELECTION,
    NAME,
    ASSEMBLY,
    VALID,
}

#endregion
