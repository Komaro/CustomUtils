using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class EditorNugetExtractService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorNugetExtractService>("NugetExtractService") : _window;

    private static PluginTreeView _activatePluginTreeView;
    private static ExtractPluginTreeView _extractPluginTreeView;

    private static readonly Dictionary<string, string> _activatePluginPathDic = new();
    private static readonly Dictionary<string, ExtractPlugin> _extractPluginDic = new();

    private static readonly List<Regex> NETSTANDARD_REGEX_LIST = new() {
        new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard2.1")),
        new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard2.0")),
        new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard1.3")),
    };

    private const string NUGET_TEMP_FOLDER = ".nuget_temp";
    
    protected override void OnEditorOpenInitialize() => CacheRefresh();
    
    [MenuItem("Service/Nuget Extract Service")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorNugetExtractService>()) {
            _activatePluginTreeView ??= new PluginTreeView();
            _activatePluginTreeView.Clear();

            _extractPluginTreeView ??= new ExtractPluginTreeView();
            _extractPluginTreeView.Clear();

            _activatePluginPathDic.Clear();
            _extractPluginDic.Clear();

            foreach (var path in AssetDatabaseUtil.GetAllPluginPaths()) {
                var name = Path.GetFileNameWithoutExtension(path);
                var version = "unknown";
                if (AssemblyProvider.GetSystemAssemblyDic().TryGetValue(name, out var assembly)) {
                    version = assembly.GetName().Version.ToString();
                }
                
                _activatePluginPathDic.TryAdd(name, path);
                _activatePluginTreeView.Add(name, version, path);
            }
            
            _activatePluginTreeView.Reload();
            _extractPluginTreeView.Reload();
        }
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("전체 플러그인", Constants.Draw.TITLE_STYLE);
        if (_activatePluginPathDic.Count > 0) {
            _activatePluginTreeView.Draw();
        } else {
            EditorGUILayout.HelpBox($"적용된 플러그인을 찾을 수 없습니다.\n{Constants.Path.PLUGINS_FULL_PATH} 경로가 존재하는지 확인이 필요합니다.", MessageType.Warning);
        }

        EditorCommon.DrawSeparator();

        using (new EditorGUILayout.HorizontalScope()) {
            var dragArea = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(dragArea, string.Empty, Constants.Draw.BOX);
            EditorGUI.LabelField(dragArea.GetCenterRect(200f, 50f), "Drag & Drop\n(Folder or ZipFile)", Constants.Draw.BOLD_CENTER_LABEL);
            HandleDragAndDrop(dragArea);

            if (_extractPluginDic.Count > 0) {
                using (new EditorGUILayout.VerticalScope()) {
                    _extractPluginTreeView.Draw();
                    if (GUILayout.Button("추출")) {
                        ExtractPlugins();
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleDragAndDrop(Rect dragArea) {
        var currentEvent = Event.current;
        switch (currentEvent.type) {
            case EventType.DragUpdated:
                if (dragArea.Contains(currentEvent.mousePosition)) {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                break;
            case EventType.DragPerform:
                if (dragArea.Contains(currentEvent.mousePosition)) {
                    DragAndDrop.AcceptDrag();
                    
                    var pathGroup = DragAndDrop.paths.GroupBy(path => {
                        if (Constants.Regex.FOLDER_PATH_REGEX.IsMatch(path) || Directory.Exists(path)) {
                            return GROUP_TYPE.DIRECTORY;
                        }

                        if (path.ContainsExtension(Constants.Extension.ZIP) || path.ContainsExtension(Constants.Extension.NUPKG)) {
                            return GROUP_TYPE.ZIP;
                        }

                        return GROUP_TYPE.NONE;
                    });
                    
                    foreach (var grouping in pathGroup) {
                        switch (grouping.Key) {
                            case GROUP_TYPE.DIRECTORY:
                                foreach (var path in grouping) {
                                    foreach (var dllFilePath in GetValidPaths(SystemUtil.FindFiles(path, Constants.Extension.DLL_FILTER, SearchOption.AllDirectories))) {
                                        var dllFileName = Path.GetFileNameWithoutExtension(dllFilePath);
                                        if (_extractPluginDic.ContainsKey(dllFileName) == false) {
                                            _extractPluginDic.Add(dllFileName, new DirectoryExtractPlugin(dllFileName, dllFilePath));
                                        }
                                    }
                                }
                                break;
                            case GROUP_TYPE.PACKAGE:
                            case GROUP_TYPE.ZIP:
                                foreach (var path in grouping) {
                                    foreach (var entry in GetValidPaths(ZipFile.Open(path, ZipArchiveMode.Read).Entries.Where(entry => entry.Name.ContainsExtension(Constants.Extension.DLL)))) {
                                        var dllFileName = Path.GetFileNameWithoutExtension(entry.Name);
                                        if (_extractPluginDic.ContainsKey(dllFileName) == false) {
                                            _extractPluginDic.Add(dllFileName, new ZipExtractPlugin(entry.Name, dllFileName, path));
                                        }
                                    }
                                }
                                break;
                                /*foreach (var path in grouping) {
                                    var copyPath = $"{Constants.Path.PROJECT_TEMP_PATH}/{Path.GetFileName(path).AutoSwitchExtension(Constants.Extension.ZIP)}";
                                    File.Move(path, copyPath);
                                    foreach (var entry in GetValidPaths(ZipFile.Open(copyPath, ZipArchiveMode.Read).Entries.Where(entry => entry.Name.ContainsExtension(Constants.Extension.DLL)))) {
                                        var dllFileName = Path.GetFileNameWithoutExtension(entry.Name);
                                        if (_extractPluginDic.ContainsKey(dllFileName) == false) {
                                            _extractPluginDic.Add(dllFileName, new PackageExtractPlugin(entry.Name, dllFileName, copyPath));
                                        }
                                    }
                                }
                                break;*/
                        }
                    }

                    _extractPluginTreeView.Clear();
                    foreach (var pair in _extractPluginDic) {
                        _extractPluginTreeView.Add(pair.Key, "", pair.Value.path);
                    }
                    
                    _extractPluginTreeView.Reload();
                    currentEvent.Use();
                }
                break;
        }
    }

    private void Temp(string zipPath, GROUP_TYPE type) {
        foreach (var entry in GetValidPaths(ZipFile.Open(zipPath, ZipArchiveMode.Read).Entries.Where(entry => entry.Name.ContainsExtension(Constants.Extension.DLL)))) {
            var dllFileName = Path.GetFileNameWithoutExtension(entry.Name);
            if (_extractPluginDic.ContainsKey(dllFileName) == false) {
                switch (type) {
                    case GROUP_TYPE.ZIP:
                        _extractPluginDic.Add(dllFileName, new ZipExtractPlugin(entry.Name, dllFileName, zipPath));
                        break;
                    case GROUP_TYPE.PACKAGE:
                        _extractPluginDic.Add(dllFileName, new PackageExtractPlugin(entry.Name, dllFileName, zipPath));
                        break;
                }
            }
        }
    }
    
    private IEnumerable<string> GetValidPaths(IEnumerable<string> filePaths) {
        foreach (var path in filePaths) {
            if (_activatePluginPathDic.ContainsKey(Path.GetFileNameWithoutExtension(path))) {
                Logger.TraceLog($"The {Path.GetFileName(path)} DLL is already imported into the project", Color.yellow);
                continue;
            }
            
            foreach (var isMatch in CheckPathMatches(path)) {
                if (isMatch) {
                    yield return path;
                    yield break;
                }
            }
        }
    }

    private IEnumerable<ZipArchiveEntry> GetValidPaths(IEnumerable<ZipArchiveEntry> entries) {
        foreach (var entry in entries) {
            if (_activatePluginPathDic.ContainsKey(Path.GetFileNameWithoutExtension(entry.FullName))) {
                Logger.TraceLog($"The {Path.GetFileName(entry.FullName)} DLL is already imported into the project", Color.yellow);
                continue;
            }
            
            foreach (var isMatch in CheckPathMatches(entry.FullName)) {
                if (isMatch) {
                    yield return entry;
                    yield break;
                }
            }
        }
    }

    private void ExtractPlugins() {
        var extractPath = $"{Constants.Path.PROJECT_TEMP_PATH}/{NUGET_TEMP_FOLDER}";
        SystemUtil.EnsureDirectoryExists(extractPath, true);
        foreach (var plugin in _extractPluginDic.Values) {
            try {
                plugin.Extract(extractPath);
            } catch (Exception ex) {
                Logger.TraceError(ex);
            }
        }
        
        var extractFiles = Directory.GetFiles(extractPath);
        if (extractFiles.Length > 0) {
            EditorCommon.ShowCheckDialogue("DLL 추출 완료", $"DLL 파일 임시 추출을 완료했습니다.\n확인시 추출 파일을 Plugins 폴더로 복사하고 임시 파일을 삭제합니다. 취소시에는 복사 없이 임시 파일만 삭제합니다.\n{extractFiles.ToStringCollection(Path.GetFileName, '\n')}", ok: () => {
                IOUtil.CopyAllFiles(extractPath, Constants.Path.PLUGINS_FULL_PATH);
                SystemUtil.DeleteDirectory(extractPath);
                AssetDatabase.Refresh();
            }, cancel:() => SystemUtil.DeleteDirectory(extractPath));
            
            _extractPluginDic.Clear();
            _extractPluginTreeView.Clear();
            _extractPluginTreeView.Reload();
        }
    }
    
    private IEnumerable<bool> CheckPathMatches(string path) => NETSTANDARD_REGEX_LIST.Select(regex => regex.IsMatch(path));

    private enum GROUP_TYPE {
        NONE,
        DIRECTORY,
        ZIP,
        PACKAGE,
    }
}

internal abstract class ExtractPlugin {

    public readonly string name;
    public readonly string path;

    public ExtractPlugin(string name, string path) {
        this.name = name;
        this.path = path;
    }

    public abstract void Extract(string destinationDirectory);
}

internal class DirectoryExtractPlugin : ExtractPlugin {

    public DirectoryExtractPlugin(string name, string path) : base(name, path) { }

    public override void Extract(string destinationDirectory) => File.Copy(path, $"{destinationDirectory}/{Path.GetFileName(path)}");
}

internal class ZipExtractPlugin : ExtractPlugin {

    protected readonly string extractTargetEntry;

    public ZipExtractPlugin(string extractTargetEntry, string name, string path) : base(name, path) => this.extractTargetEntry = extractTargetEntry;

    public override void Extract(string destinationDirectory) {
        var entry = ZipFile.Open(path, ZipArchiveMode.Read).Entries.FirstOrDefault(entry => entry.Name.EqualsFast(extractTargetEntry));
        entry?.ExtractToFile($"{destinationDirectory}/{Path.GetFileName(entry.Name)}");
    }
}

internal class PackageExtractPlugin : ZipExtractPlugin {

    public PackageExtractPlugin(string extractTargetEntry, string name, string path) : base(extractTargetEntry, name, path) { }

    public override void Extract(string destinationDirectory) {
        base.Extract(destinationDirectory);
        File.Delete(path);
    }
}

#region [TreeView]

internal class PluginTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("명칭", 200f),
        CreateColumn("버전", 50f, 100f),
        CreateColumn("경로", 350f),
    };

    protected readonly GenericMenu menu;
    
    public PluginTreeView(MultiColumnHeaderState.Column[] columns) : base(columns) { }
    
    public PluginTreeView() : base(COLUMNS) {
        menu = new GenericMenu();
        menu.AddItem(new GUIContent("Copy Name"), false, () => {
            if (itemList[GetSelection().First()] is PluginTreeViewItem item) {
                EditorGUIUtility.systemCopyBuffer = item.name;
            }    
        });
        
        menu.AddItem(new GUIContent("Copy Version"), false, () => {
            if (itemList[GetSelection().First()] is PluginTreeViewItem item) {
                EditorGUIUtility.systemCopyBuffer = item.version;
            }
        });
        
        menu.AddItem(new GUIContent("Select File"), false, () => {
            if (itemList[GetSelection().First()] is PluginTreeViewItem item && AssetDatabaseUtil.TryLoadFromPath(item.path, out var asset)) {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        });
    }

    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100f), GUILayout.MaxHeight(300f))));
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is PluginTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.name);
            EditorGUI.LabelField(args.GetCellRect(2), item.version);
            EditorGUI.LabelField(args.GetCellRect(3), item.path);
            
            if (args.rowRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ContextClick) {
                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    }

    public void Add(string name, string version, string path) => itemList.Add(new PluginTreeViewItem(itemList.Count, name, version, path));

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        if (rootItem.children.TryCast<PluginTreeViewItem>(out var enumerable) && EnumUtil.TryConvertFast<SORT_TYPE>(index, out var type)) {
            return type switch {
                SORT_TYPE.NO => enumerable.OrderBy(x => x.id, isAscending),
                SORT_TYPE.NAME => enumerable.OrderBy(x => x.name, isAscending),
                SORT_TYPE.PATH => enumerable.OrderBy(x => x.path, isAscending),
                _ => enumerable
            };
        }

        return Enumerable.Empty<TreeViewItem>();
    }

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => item is PluginTreeViewItem packageItem && packageItem.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    protected sealed class PluginTreeViewItem : TreeViewItem {

        public string name;
        public string version;
        public string path;
        
        public PluginTreeViewItem(int id, string name, string version, string path) {
            this.id = id;
            this.name = name;
            this.version = version;
            this.path = path;
        }
    }

    protected enum SORT_TYPE {
        NO,
        NAME,
        PATH,
    }
}

internal class ExtractPluginTreeView : PluginTreeView {
    
    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("명칭", 200f),
        CreateColumn("경로", 350f),
    };
    
    public ExtractPluginTreeView() : base(COLUMNS) { }
    
    public override void Draw() {
        using (new EditorGUILayout.VerticalScope()) {
            EditorGUILayout.LabelField("추출 대기");
            searchString = searchField.OnToolbarGUI(searchString);
            OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100f), GUILayout.MaxHeight(300f))));
        }
    }
    
    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is PluginTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.name);
            EditorGUI.LabelField(args.GetCellRect(2), item.path);
        }
    }
}

#endregion