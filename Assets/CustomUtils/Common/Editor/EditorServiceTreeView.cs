using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public abstract class EditorServiceTreeView : TreeView {

    protected readonly SearchField searchField = new();

    protected List<TreeViewItem> itemList = new();

    protected OverridenMethod overridenMethod;

    public EditorServiceTreeView(MultiColumnHeaderState.Column[] columns) : this(new MultiColumnHeaderState(columns)) { }
    public EditorServiceTreeView(MultiColumnHeaderState headerState) : this(new MultiColumnHeader(headerState)) { }
    
    public EditorServiceTreeView(MultiColumnHeader header) : base(new TreeViewState()) {
        overridenMethod = new OverridenMethod(GetType(), nameof(OnSortingChanged), nameof(RowGUI), nameof(GetOrderBy));
        
        multiColumnHeader = header;
        multiColumnHeader.ResizeToFit();

        if (overridenMethod.HasOverriden(nameof(GetOrderBy))) {
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }

        if (overridenMethod.HasOverriden(nameof(RowGUI)) == false) {
            Logger.TraceLog($"{nameof(RowGUI)} has not been overridden.", Color.red);
        }
    }

    protected override TreeViewItem BuildRoot() => new() { id = 0, depth = -1, children = itemList };
    
    public void Clear() => itemList.Clear();
    
    public virtual void Draw() {
        searchString = searchField.OnToolbarGUI(searchString);
        OnGUI(new Rect(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(200f), GUILayout.MaxHeight(500f))));
    }
    
    protected bool TryGetOrderBy(int index, bool isAscending, out IEnumerable<TreeViewItem> enumerable) {
        enumerable = GetOrderBy(index, isAscending);
        return enumerable.Equals(Enumerable.Empty<TreeViewItem>()) == false;
    }

    protected virtual IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) => Enumerable.Empty<TreeViewItem>();

    protected virtual void OnSortingChanged(MultiColumnHeader header) {
        if (overridenMethod.HasOverriden(nameof(GetOrderBy)) == false) {
            Logger.TraceLog($"{nameof(GetOrderBy)} has not been overridden.", Color.red);
            return;
        }
        
        var rows = GetRows();
        if (rows.Count <= 1 || header.sortedColumnIndex == -1) {
            return;
        }
            
        var sortedColumns = header.state.sortedColumns;
        if (sortedColumns.Length <= 0) {
            return;
        }

        var isAscending = multiColumnHeader.IsSortedAscending(sortedColumns.First());
        if (TryGetOrderBy(sortedColumns.First(), isAscending, out var enumerable)) {
            rows.Clear();
            rootItem.children = enumerable.ToList();
            rootItem.children.ForEach(x => rows.Add(x));
            Repaint();
        }
    }
    
    public static MultiColumnHeaderState.Column CreateColumn(string headerContent, float minWidth = 20f, float maxWidth = 1000000f, TextAlignment textAlignment = TextAlignment.Center) => new() {
        headerContent = new GUIContent(headerContent),
        headerTextAlignment = textAlignment,
        allowToggleVisibility = false,
        minWidth = minWidth,
        maxWidth = maxWidth,
        autoResize = true,
        canSort = true
    };
}