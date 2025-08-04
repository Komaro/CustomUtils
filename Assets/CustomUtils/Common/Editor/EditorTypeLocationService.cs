using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Color = System.Drawing.Color;

public partial class EditorTypeLocationService : EditorService<EditorTypeLocationService> {

    private static bool _isActiveAnalyze;
    private static bool _isActiveFullProcessor;

    private readonly BoolSwitchText _fullProcessInfoText = new($"사용 가능한 모든 프로세서를 사용합니다\n프로세서의 갯수 : {Environment.ProcessorCount}", $"사용 가능한 모든 프로세서 중 절반만 사용합니다\n프로세서의 갯수 : {Environment.ProcessorCount / 2}");
    
    private SerializedAssemblyArray _ignoreAssemblyAssets;

    private TypeLocationTreeView _treeView;
    
    private EditorAsyncOperation _operation;
    
    private const string DEFAULT_PATH = "Service/TypeLocation";
    private const string WINDOW_PATH = DEFAULT_PATH + "/Type Location Service";
    
    private const string ACTIVE_ANALYZE = DEFAULT_PATH + "ActiveAnalzye";
    private const string ACTIVE_FULL_PROCESSOR = DEFAULT_PATH + "ActiveFullProcessor";

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        _isActiveAnalyze = EditorPrefsUtil.GetBool(ACTIVE_ANALYZE);
        _isActiveFullProcessor = EditorPrefsUtil.GetBool(ACTIVE_FULL_PROCESSOR);
    }

    private static void OnAfterAssemblyReload() {
        if (_isActiveAnalyze) {
            AnalyzeTypeLocation();
        }
    }

    [MenuItem(WINDOW_PATH)]
    private static void OpenWindow() => Window.Open();
    protected override void Refresh() => _operation = AsyncRefresh();

    protected override async Task AsyncRefresh(EditorAsyncOperation operation) {
        _fullProcessInfoText.SwitchValue = _isActiveFullProcessor;
        
        operation.Init();
        
        _ignoreAssemblyAssets ??= CreateInstance<SerializedAssemblyArray>();
        _treeView ??= new TypeLocationTreeView();
        
        _treeView.Clear();
        if (_isActiveAnalyze == false) {
            operation.Done();
            _treeView.Reload();
        } else {
            if (IsValid() == false) {
                while (IsRunning == false) {
                    await Task.Delay(50);
                }
                
                while (IsRunning) {
                    await Task.Delay(50);
                }
            }
            
            var progress = 0;
            foreach (var (type, (path, line)) in _typeLocationDic) {
                _treeView.Add(type, path, line);
                operation.Report(++progress, _typeLocationDic.Count);
            }
            
            _treeView.Reload();
            operation.Done();
            
            await Task.CompletedTask;
        }
    }


    private void OnGUI() {
        EditorGUILayout.Space(10);
        
        if (_operation is { IsDone: false }) {
            EditorCommon.DrawProgressBar(_operation);
            Repaint();
            return;
        }

        EditorCommon.DrawLabelToggle(ref _isActiveAnalyze, "타입 분석 활성화", () => EditorPrefsUtil.Set(ACTIVE_ANALYZE, _isActiveAnalyze));
        if (_isActiveAnalyze == false) {
            EditorGUILayout.HelpBox("타입 분석이 활성화 되어있지 않습니다", MessageType.Warning);
            return;
        }

        EditorCommon.DrawLabelToggle(ref _isActiveFullProcessor, "전체 프로세서 사용", () => {
            EditorPrefsUtil.Set(ACTIVE_FULL_PROCESSOR, _isActiveFullProcessor);
            _fullProcessInfoText.SwitchValue = _isActiveFullProcessor;
        });
        
        EditorGUILayout.HelpBox(_fullProcessInfoText, MessageType.Info);

        EditorCommon.DrawSeparator();
        EditorGUI.BeginDisabledGroup(IsRunning);
        
        if (GUILayout.Button("타입 재분석", GUILayout.Height(50f))) {
            AnalyzeTypeLocation(true);
            Refresh();
        }
        
        if (_ignoreAssemblyAssets.Value != null) {
            EditorGUILayout.LabelField("어셈블리 제외 리스트", Constants.Draw.AREA_TITLE_STYLE);
            _ignoreAssemblyAssets.Draw();
        }
        
        if (_operation.IsDone) {
            EditorCommon.DrawSeparator();
            _treeView.Draw();
        } else {
            EditorGUILayout.HelpBox("타입을 다시 분석하는 중입니다...", MessageType.Warning);
        }
        
        EditorGUI.EndDisabledGroup();
    }

    public static bool TryGetTypeLocation(Type type, out (string path, int line) location) => (location = GetTypeLocation(type)) != default; 
    public static (string path, int line) GetTypeLocation(Type type) => _typeLocationDic.TryGetValue(type, out var location) ? location : default;

    public static bool ContainsTypeLocation(Type type) => _typeLocationDic.ContainsKey(type);

    public static bool IsValid() => _typeLocationDic.Count > 0;

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

        public void Load() {
            if (EditorPrefsUtil.TryGet(IGNORE_ASSEMBLIES_KEY, out string[] ignoreAssemblies)) {
                Value = AssetDatabaseUtil.FindAssets<AssemblyDefinitionAsset>(FilterUtil.CreateFilter(AreaFilter.all)).Where(asset => ignoreAssemblies.Contains(asset.name)).ToArray();
            }
            
            serializedObj.Update();
        }

        protected override void OnChanged(AssemblyDefinitionAsset value, int index) => Save();
    }
}


// Thread

#region [TreeView]

internal class TypeLocationTreeView : OptimizeEditorServiceTreeView<TypeLocationTreeView.Data> {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = { 
        CreateColumn("타입", 105f),
        CreateColumn("어셈블리", 150f),
    };
    
    public TypeLocationTreeView() : base(COLUMNS) { }

    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(200f), GUILayout.MaxHeight(500f))));

        if (HasSelection() == false || TryFindDataFromId(GetSelection()[0], out var data) == false) {
            return;
        }
        
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            EditorCommon.DrawLabelTextField("경로", data.Path);
        }
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (dataDic.TryGetValue(args.item.id, out var data)) {
            EditorGUI.LabelField(args.GetCellRect(0), data.Name);
            EditorGUI.LabelField(args.GetCellRect(1), data.AssemblyName);
        }
    }

    public void Add(Type type, string path, int line) => Add(new Data(itemList.Count, type.GetCleanFullName(), type.Assembly.GetName().Name, path, line));

    protected override void DoubleClickedItem(int id) {
        if (EditorTypeLocationService.IsValid() == false) {
            Logger.TraceLog($"{nameof(EditorTypeLocationService)} is not valid. To enable the redirect feature, the {nameof(EditorTypeLocationService)} must be enabled.", Color.Yellow);
            return;
        }
        
        if (TryFindDataFromId(id, out var data) == false) {
            Logger.TraceError($"{nameof(data)} not found for id: {id}");
            return;
        }

        if (IsValidRedirect(data) == false) {
            Logger.TraceError($"Invalid redirect path || {data.Path}  [{data.Line}]");
            return;
        }

        InternalEditorUtility.OpenFileAtLineExternal(data.Path, data.Line);
    }

    private bool IsValidRedirect(Data data) => File.Exists(data.Path) && data.Line > 0;

    protected override int ItemComparision(int index, bool isAscending, TreeViewItem xItem, TreeViewItem yItem) {
        if (isAscending) {
            (xItem, yItem) = (yItem, xItem);
        }

        return TryFindData(xItem, out var xData) && TryFindData(yItem, out var yData)
            ? (SORT_TYPE) index switch {
                SORT_TYPE.NAME => string.CompareOrdinal(xData.Name, yData.Name),
                SORT_TYPE.ASSEMBLY_NAME => string.CompareOrdinal(xData.AssemblyName, yData.AssemblyName),
                SORT_TYPE.PATH => string.CompareOrdinal(xData.Path, yData.Path),
                _ => 0
            } : 1;
    }

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => TryFindDataFromId(item.id, out var data) && (data.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || data.AssemblyName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

    public record Data : TreeViewItemData {

        public string Name { get; }
        public string AssemblyName { get; }
        public string Path { get; }
        public int Line { get; }
        
        public Data(int id, string name, string assemblyName, string path, int line) : base(id) {
            Name = name;
            AssemblyName = assemblyName;
            Path = path;
            Line = line;
        }
    }
    

    private enum SORT_TYPE {
        NAME,
        ASSEMBLY_NAME,
        PATH,
    }
}

#endregion