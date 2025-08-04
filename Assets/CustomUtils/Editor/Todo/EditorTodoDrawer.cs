using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

    protected T treeView;
    
    protected EditorTodoDrawer(EditorWindow window) : base(window) {
        
    }

    public override void CacheRefresh() {
        treeView ??= SystemUtil.SafeCreateInstance<T>();
        treeView.Clear();
    }
}


[EditorTodoDrawer(typeof(TestRequiredAttribute))]
public class EditorTestRequiredDrawer : EditorTodoDrawer<TodoTestRequiredTreeView> {

    public EditorTestRequiredDrawer(EditorWindow window) : base(window) {
        
    }

    public override void CacheRefresh() {
        base.CacheRefresh();
        // TODO. 현재 class type만 캐싱 중. Type이 아닌 MemberInfo 형태로 획득할 수 있도록 수정 필요
        
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
            EditorGUI.LabelField(args.GetCellRect(0), data.Id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), data.TestType.ToString());
            EditorGUI.LabelField(args.GetCellRect(2), data.ClassName);
        }
    }
    protected override string GetDescription(int index) => TryFindData(index, out var data) ? data.Description : string.Empty;

    protected override int ItemComparision(int index, bool isAscending, TreeViewItem xItem, TreeViewItem yItem) {
        if (isAscending) {
            (xItem, yItem) = (yItem, xItem);
        }

        return TryFindData(xItem, out var xData) && TryFindData(yItem, out var yData)
            ? (SORT_TYPE) index switch {
                SORT_TYPE.NO => xData.Id.CompareTo(yData.Id),
                SORT_TYPE.TYPE => xData.TestType.CompareTo(yData.TestType),
                SORT_TYPE.CLASS => string.CompareOrdinal(xData.ClassName, yData.ClassName),
                _ => 0
            }
            : 1;
    }

    protected override void DoubleClickedItem(int id) {
        if (TryFindDataFromId(id, out var data) == false) {
            Logger.TraceError($"{nameof(data)} not found for id: {id}");
            return;
        }

        if (EditorTypeLocationService.IsValid() == false) {
            Logger.TraceLog($"{nameof(EditorTypeLocationService)} is not valid. To enable the redirect feature, the {nameof(EditorTypeLocationService)} must be enabled.", Color.yellow);
            return;
        }

        if (EditorTypeLocationService.TryGetTypeLocation(data.ClassType, out var location) == false) {
            Logger.TraceError($"{nameof(location)} not found for type: {data.ClassName}");
            return;
        }

        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(location.path, location.line);
    }

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => TryFindData(item.id, out var data) && data.ClassName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    public record Data : TreeViewItemData {

        public TEST_TYPE TestType { get; }
        public Type ClassType { get; }
        public string ClassName { get; }
        public string Description { get; }

        public Data(int id, TestRequiredAttribute attribute, Type classType) : base(id) {
            TestType = attribute.type;
            ClassType = classType;
            ClassName = classType.GetCleanFullName();
            Description = attribute.description;
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