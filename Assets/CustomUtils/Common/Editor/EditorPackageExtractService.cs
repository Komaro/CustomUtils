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

public class EditorPackageExtractService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorPackageExtractService>("PackageExtractService") : _window;

    private static PackageTreeView _packageTreeView;

    private static Dictionary<string, string> _pluginPathDic = new();
    
    private static readonly Regex NETSTANDARD_2_1_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard2.1"));
    private static readonly Regex NETSTANDARD_2_0_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard2.0"));
    private static readonly Regex NETSTANDARD_1_3_REGEX = new(string.Format(Constants.Regex.FOLDER_CONTAINS_FORMAT, "netstandard1.3"));

    protected override void OnEditorOpenInitialize() => CacheRefresh();
    
    
    [MenuItem("Service/Package Extract Service")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorPackageExtractService>()) {
            _packageTreeView ??= new PackageTreeView();
            _packageTreeView.Clear();

            _pluginPathDic.Clear();
            foreach (var path in AssetDatabaseUtil.GetAllPluginPaths()) {
                var name = Path.GetFileNameWithoutExtension(path);
                _pluginPathDic.TryAdd(name, path);
                _packageTreeView.Add(name, path);
            }
            
            _packageTreeView.Reload();
        }
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("전체 플러그인", Constants.Draw.TITLE_STYLE);
        if (_pluginPathDic.Count > 0) {
            _packageTreeView.Draw();
        } else {
            EditorGUILayout.HelpBox($"적용된 플러그인을 찾을 수 없습니다.\n{Constants.Path.PLUGINS_FULL_PATH} 경로가 존재하는지 확인이 필요합니다.", MessageType.Warning);
        }

        EditorCommon.DrawSeparator();
        
        var dragArea = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(dragArea, string.Empty, Constants.Draw.BOX);
        EditorGUI.LabelField(dragArea.GetCenterRect(200f, 50f), "Drag & Drop\n(Folder or ZipFile)", Constants.Draw.BOLD_CENTER_LABEL);
        HandleDragAndDrop(dragArea);
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
                    var pathGroup = DragAndDrop.paths.GroupBy(path => 
                        Constants.Regex.FOLDER_PATH_REGEX.IsMatch(path) || Directory.Exists(path) ? GROUP_TYPE.DIRECTORY : 
                        path.ContainsExtension(Constants.Extension.ZIP) ? GROUP_TYPE.ZIP : GROUP_TYPE.NONE);

                    var extractPath = $"{Constants.Path.PROJECT_TEMP_PATH}/.temp/";
                    SystemUtil.EnsureDirectoryExists(extractPath);
                    foreach (var grouping in pathGroup) {
                        switch (grouping.Key) {
                            case GROUP_TYPE.DIRECTORY:
                                foreach (var path in grouping) {
                                    foreach (var dllFilePath in GetValidPaths(SystemUtil.FindFiles(path, Constants.Extension.DLL_FILTER))) {
                                        File.Copy(dllFilePath, $"{extractPath}{Path.GetFileName(dllFilePath)}");
                                    }
                                }
                                break;
                            case GROUP_TYPE.ZIP:
                                foreach (var path in grouping) {
                                    foreach (var entry in GetValidPaths(ZipFile.Open(path, ZipArchiveMode.Read).Entries.Where(entry => entry.Name.ContainsExtension(Constants.Extension.DLL)))) {
                                        entry.ExtractToFile($"{extractPath}{entry.Name}", true);
                                    }
                                }
                                break;
                            default:
                                Logger.TraceLog("Invalid Path\n" + grouping.ToStringCollection('\n'));
                                break;
                        }
                    }
                    
                    SystemUtil.CopyAllFiles(extractPath, $"{Constants.Path.PLUGINS_FULL_PATH}");
                    
                    SystemUtil.DeleteDirectory(extractPath);
                    currentEvent.Use();
                }
                break;
        }
    }
    
    private IEnumerable<string> GetValidPaths(IEnumerable<string> filePaths) {
        foreach (var path in filePaths) {
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
            foreach (var isMatch in CheckPathMatches(entry.FullName)) {
                if (isMatch) {
                    yield return entry;
                    yield break;
                }
            }
        }
    }
    
    private IEnumerable<bool> CheckPathMatches(string path) {
        yield return NETSTANDARD_2_1_REGEX.IsMatch(path);
        yield return NETSTANDARD_2_0_REGEX.IsMatch(path);
        yield return NETSTANDARD_1_3_REGEX.IsMatch(path);
    }

    private enum GROUP_TYPE {
        NONE,
        DIRECTORY,
        ZIP
    }
}


#region [TreeView]

internal class PackageTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("명칭", 200f),
        CreateColumn("경로", 350f),
    };

    public PackageTreeView() : base(COLUMNS) { }

    public override void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100f), GUILayout.MaxHeight(300f))));
    }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is PackageTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.name);
            EditorGUI.LabelField(args.GetCellRect(2), item.path);
        }
    }

    public void Add(string name, string path) => itemList.Add(new PackageTreeViewItem(itemList.Count, name, path));

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        if (rootItem.children.TryCast<PackageTreeViewItem>(out var enumerable) && EnumUtil.TryConvertFast<SORT_TYPE>(index, out var type)) {
            return type switch {
                SORT_TYPE.NO => enumerable.OrderBy(x => x.id, isAscending),
                SORT_TYPE.NAME => enumerable.OrderBy(x => x.name, isAscending),
                SORT_TYPE.PATH => enumerable.OrderBy(x => x.path, isAscending),
                _ => enumerable
            };
        }

        return Enumerable.Empty<TreeViewItem>();
    }

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => item is PackageTreeViewItem packageItem && packageItem.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    protected sealed class PackageTreeViewItem : TreeViewItem {

        public string name;
        public string path;
        
        public PackageTreeViewItem(int id, string name, string path) {
            this.id = id;
            this.name = name;
            this.path = path;
        }
    }

    private enum SORT_TYPE {
        NO,
        NAME,
        PATH,
    }
}

#endregion