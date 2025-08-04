using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

public abstract record TreeViewItemData(int Id) {

    public int Id { get; protected set; } = Id;
}

[RefactoringRequired("EditorServiceTreeViewItem 종송성 제거 후 대체 예정")]
public abstract class OptimizeEditorServiceTreeView<TData> : EditorServiceTreeView where TData : TreeViewItemData {
    
    protected readonly List<TreeViewItem> itemList = new();
    protected readonly Dictionary<int, TData> dataDic = new();

    protected readonly AutoOverridenMethod autoOverridenMethod;

    private bool isForceReload;

    protected OptimizeEditorServiceTreeView(MultiColumnHeaderState.Column[] columns) : base(columns) {
        autoOverridenMethod = new AutoOverridenMethod(GetType());
    }

    protected override TreeViewItem BuildRoot() => isForceReload ? CreateRoot() : rootItem ?? CreateRoot();
    protected virtual TreeViewItem CreateRoot() => new TreeViewItem { id = 0, depth = -1, children = itemList };

    public void ForceReload() {
        isForceReload = true;
        Reload();
        isForceReload = false;
    } 
    
    public void Add(TData data) {
        if (dataDic.TryAdd(data.Id, data)) {
            Add(new TreeViewItem(data.Id));
        }
    }

    protected override void Add(TreeViewItem item) => itemList.Add(item);
    
    protected override void OnSortingChanged(MultiColumnHeader header) {
        if (autoOverridenMethod.HasOverriden(nameof(ItemComparision)) == false) {
            Logger.TraceLog($"{nameof(ItemComparision)} has not been overridden.", Color.red);
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

        var columnIndex = sortedColumns.First();
        rows.Clear();
        itemList.Sort((xItem, yItem) => ItemComparision(columnIndex, multiColumnHeader.IsSortedAscending(columnIndex), xItem, yItem));
        BuildRows(rootItem);
        Repaint();
    }

    protected virtual int ItemComparision(int index, bool isAscending, TreeViewItem xItem, TreeViewItem yItem) => throw new MissingMethodException(GetType().GetCleanFullName(), MethodBase.GetCurrentMethod().GetCleanFullName());

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        itemList.Sort();
        return Enumerable.Empty<TreeViewItem>();
    }

    protected bool TryFindData(TreeViewItem item, out TData data) => (data = FindData(item)) != null;
    protected TData FindData(TreeViewItem item) => dataDic.TryGetValue(item.id, out var data) ? data : null;
    
    protected bool TryFindData(int index, out TData data) => (data = FindData(index)) != null;
    protected TData FindData(int index) => itemList.TryFind(index, out var item) && dataDic.TryGetValue(item.id, out var data) ? data : null;

    protected bool TryFindDataFromId(int id, out TData data) => (data = FindDataFromId(id)) != null;
    protected TData FindDataFromId(int id) => dataDic.TryGetValue(id, out var data) ? data : null;
}


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
            Logger.TraceLog($"{nameof(RowGUI)} has not been overridden", Color.red);
        }
    }

    protected override TreeViewItem BuildRoot() => new() { id = 0, depth = -1, children = itemList };
    protected sealed override bool DoesItemMatchSearch(TreeViewItem item, string search) => OnDoesItemMatchSearch(item, search);

    protected virtual void Add(TreeViewItem item) => itemList.Add(item);
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

    protected virtual bool OnDoesItemMatchSearch(TreeViewItem item, string search) {
        if (string.IsNullOrEmpty(item.displayName)) {
            return true;
        }

        return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase)>= 0;
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