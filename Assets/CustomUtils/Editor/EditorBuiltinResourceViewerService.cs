using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class EditorBuiltinResourceViewerService : EditorWindow {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorBuiltinResourceViewerService>("Builtin Resource Viewer Service") : _window;

    private static SearchField _searchField;
    private static MultiColumnHeader _header;
    private static BuiltinResourceTreeView _treeView;
    
    private static readonly Dictionary<int, Texture2D> _textureCacheDic = new();

    [MenuItem("Service/Resource/Builtin Resource Viewer Service")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        _header = new MultiColumnHeader(new MultiColumnHeaderState(new[] {
            new MultiColumnHeaderState.Column {
                headerContent = EditorGUIUtility.IconContent("FilterByType"),
                autoResize = false,
                minWidth = 40f,
                width = 40f,
                headerTextAlignment = TextAlignment.Center,
                allowToggleVisibility = false,
                canSort = false,
            },
            new MultiColumnHeaderState.Column {
                headerContent = new GUIContent("Content"),
                width = 300f,
                minWidth = 300f,
                allowToggleVisibility = false,
                canSort = true,
            }
        }));

        _searchField ??= new SearchField();
        _treeView ??= new BuiltinResourceTreeView(new TreeViewState(), _header);
        
        if (_textureCacheDic.Any()) {
            _treeView.Reload();
            return;
        }
        
        _textureCacheDic.Clear();
        Debug.unityLogger.logEnabled = false;
        var list = Resources.FindObjectsOfTypeAll<Texture2D>();
        foreach (var texture in list) {
            if (TryGetGUIContent(texture, out var content)) {
                _textureCacheDic.AutoAdd(content.GetHashCode(), texture);
            }
        }
        Debug.unityLogger.logEnabled = true;
        
        foreach (var pair in _textureCacheDic) {
            _treeView.Add(new TreeViewItem(pair.Key, 0, pair.Value.name) { icon = pair.Value });
        }
        
        _treeView.Reload();

        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    private void OnGUI() {
        _treeView.searchString = _searchField.OnGUI(_treeView.searchString);
        _treeView.OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))));
    }

    private static bool TryGetGUIContent(Texture2D texture, out GUIContent content) {
        if (texture.hideFlags != HideFlags.HideAndDontSave && texture.hideFlags != (HideFlags.HideInInspector | HideFlags.HideAndDontSave)) {
            content = null;
            return false;
        }
        
        content = EditorGUIUtility.IconContent(texture.name);
        return content != null && content.image != null;
    }
    
    private class BuiltinResourceTreeView : TreeView {

        private int _iconColumnWidth = 0;
        private readonly List<TreeViewItem> _itemList = new();

        public BuiltinResourceTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header) { }
        protected override TreeViewItem BuildRoot() => new() { id = 0, depth = -1, children = _itemList };

        public override void OnGUI(Rect rect) {
            if (_iconColumnWidth != (int) multiColumnHeader.state.columns[0].width) {
                RefreshCustomRowHeights();
            }
            
            base.OnGUI(rect);
        }
        
        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            var width = Mathf.Min(multiColumnHeader.state.columns[0].width - 2 * cellMargin, item.icon.width);
            var height = width * item.icon.height / item.icon.width;
            return Mathf.Max(height, rowHeight);
        }
        
        protected override void RowGUI(RowGUIArgs args) {
            var item = args.item;
            GUI.DrawTexture(GetCenterRect(args.GetCellRect(0), item.icon.width, item.icon.height), item.icon, ScaleMode.ScaleToFit);
            
            var nameRect = args.GetCellRect(1);
            EditorGUI.LabelField(nameRect, item.displayName);
            if (nameRect.Contains(Event.current.mousePosition)) {
                if (Event.current.type == EventType.ContextClick) {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Copy"), false, () => EditorGUIUtility.systemCopyBuffer = item.displayName);
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                if (Event.current.clickCount > 1) {
                    EditorGUIUtility.systemCopyBuffer = item.displayName;
                }
            }
        }
        
        private Rect GetCenterRect(Rect rect, float width, float height) {
            if (width < rect.width) {
                var diff = rect.width - width;
                rect.xMin += diff * .5f;
                rect.xMax -= diff * .5f;
            }
            
            if (height < rect.height) {
                var diff = rect.height - height;
                rect.yMin += diff * .5f;
                rect.yMax -= diff * .5f;
            }

            return rect;
        }
        
        public void Add(TreeViewItem item) => _itemList.Add(item);
        public void Clear() => _itemList.Clear();
    }
}
