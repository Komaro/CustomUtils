using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Attribute = System.Attribute;

[AttributeUsage(AttributeTargets.Class)]
public class EditorTodoDrawerAttribute : Attribute {

    public readonly Type attributeType;
    
    public EditorTodoDrawerAttribute(Type attributeType) => this.attributeType = attributeType;
}


[RequiresAttributeImplementation(typeof(EditorTodoDrawerAttribute))]
public abstract class EditorTodoDrawer<T> : EditorDrawer where T : EditorServiceTreeView, new() {

    protected readonly T treeView;
    
    protected EditorTodoDrawer(EditorWindow window) : base(window) {
        treeView = SystemUtil.SafeCreateInstance<T>();
    }

    public override void CacheRefresh() {
        treeView.Clear();
    }
}


[EditorTodoDrawer(typeof(TestRequiredAttribute))]
public class EditorTestRequiredDrawer : EditorTodoDrawer<TodoTestRequiredTreeView> {

    public EditorTestRequiredDrawer(EditorWindow window) : base(window) {
        
    }

    public override void CacheRefresh() {
        base.CacheRefresh();
        foreach (var (attribute, type) in ReflectionProvider.GetAttributeTypeSets<TestRequiredAttribute>()) {
            treeView.Add(attribute, type);
        }
        
        treeView.Reload();
    }

    public override void Draw() {
        treeView.Draw();
    }
}

// [EditorTodoDrawer(typeof(RefactoringRequiredAttribute))]
// public class EditorRefactoringRequiredDrawer : EditorTodoDrawer {
//
//     public EditorRefactoringRequiredDrawer(EditorWindow window) : base(window) {
//         
//     }
//
//     public override void Draw() {
//         EditorCommon.DrawFitLabel(nameof(EditorRefactoringRequiredDrawer));
//     }
// }
//
// [EditorTodoDrawer(typeof(TempMethodAttribute))]
// public class EditorTempMethodDrawer : EditorTodoDrawer {
//
//     public EditorTempMethodDrawer(EditorWindow window) : base(window) {
//         
//     }
//
//     public override void Draw() {
//         EditorCommon.DrawFitLabel(nameof(EditorTempMethodDrawer));
//     }
// }
//
// [EditorTodoDrawer(typeof(TempClassAttribute))]
// public class EditorTempClassDrawer : EditorTodoDrawer {
//
//     public EditorTempClassDrawer(EditorWindow window) : base(window) {
//         
//     }
//
//     public override void Draw() {
//         EditorCommon.DrawFitLabel(nameof(EditorTempClassDrawer));
//     }
// }


#region [TreeView]

public abstract class TodoTreeView<TData> : OptimizeEditorServiceTreeView<TData> where TData : TreeViewItemData {
    
    public TodoTreeView(MultiColumnHeaderState.Column[] columns) : base(columns) { }

    public override void OnGUI(Rect rect) {
        using (new EditorGUILayout.VerticalScope()) {
            base.OnGUI(rect);
            EditorCommon.DrawSeparator();
            var selection = GetSelection().FirstOrDefault();
            if (IsValidIndex(selection)) {
                EditorCommon.DrawWideTextArea("설명", GetDescription(selection), 300f);
            }
        }
    }

    protected abstract string GetDescription(int index);

    protected bool IsValidIndex(int index) => itemList.Count > index;
}

public class TodoTestRequiredTreeView : TodoTreeView<TodoTestRequiredTreeView.Data> {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = { 
        CreateColumn("No", 35f, 35f),
        CreateColumn("타입", 100f, 120f),
        CreateColumn("대상 클래스", 200f),
    };

    public TodoTestRequiredTreeView() : base(COLUMNS) { }

    public void Add(TestRequiredAttribute attribute, Type classType) => Add(new Data(itemList.Count, attribute, classType));

    protected override void RowGUI(RowGUIArgs args) {
        if (dataDic.TryGetValue(args.item.id, out var data)) {
            EditorGUI.LabelField(args.GetCellRect(0), data.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), data.type.ToString());
            EditorGUI.LabelField(args.GetCellRect(2), data.className);
        }
    }
    protected override string GetDescription(int index) => TryFindData(index, out var data) ? data.description : string.Empty;

    protected override int Sort(int index, bool isAscending, TreeViewItem xItem, TreeViewItem yItem) {
        if (isAscending) {
            (xItem, yItem) = (yItem, xItem);
        }

        return TryFindData(xItem, out var xData) && TryFindData(yItem, out var yData)
            ? (SORT_TYPE) index switch {
                SORT_TYPE.NO => xData.id.CompareTo(yData.id),
                SORT_TYPE.TYPE => xData.type.CompareTo(yData.type),
                SORT_TYPE.CLASS => string.CompareOrdinal(xData.className, yData.className),
                _ => 0
            }
            : 1;
    }

    protected override void DoubleClickedItem(int id) {
        if (TryFindDataFromId(id, out var data)) {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(data.className, 1);
        }
    }

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => TryFindData(item.id, out var data) && data.className.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    public record Data : TreeViewItemData {

        public readonly TEST_TYPE type;
        public readonly string className;
        public readonly string description;

        public Data(int id, TestRequiredAttribute attribute, Type classType) : base(id) {
            type = attribute.type;
            description = attribute.description;
            className = classType.GetCleanFullName();
        }
    }
    
    // public sealed class Item : TreeViewItem {
    //
    //     public readonly TEST_TYPE type;
    //     public readonly string className;
    //     public readonly string description;
    //
    //     public Item(int id, TestRequiredAttribute attribute, Type classType) {
    //         this.id = id;
    //         type = attribute.type;
    //         description = attribute.description;
    //         this.className = classType.GetCleanFullName();
    //     }
    // }

    private enum SORT_TYPE {
        NO,
        TYPE,
        CLASS,
    }
}

#endregion