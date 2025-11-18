using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[TestRequired]
public abstract class SerializedArray<T> : SerializedObject<T[]> {
    
    protected ReorderableList list;

    public T this[int index] {
        get => Value.IsValidIndex(index) ? Value[index] : default;
        set {
            if (Value.IsValidIndex(index)) {
                Value[index] = value;
                serializedObj.Update();
            }
        }
    }

    protected override void OnEnable() {
        if (isInitialized == false) {
            serializedObj = new SerializedObject(this);
            serializedProperty = serializedObj.FindProperty(nameof(Value));
            
            list = new ReorderableList(serializedObj, serializedProperty, false, false, true, true);
            list.onAddCallback += OnAdd;
            list.onRemoveCallback += OnRemove;
            list.drawElementCallback += OnElementDraw;
            
            isInitialized = true;
        }
    }

    public override void Draw() {
        if (isInitialized) {
            list.DoLayoutList();
        } else {
            EditorCommon.DrawFitLabel("초기화중...");
        }
    }

    protected virtual void OnElementDraw(Rect rect, int index, bool isActive, bool isFocused) {
        EditorGUI.BeginChangeCheck();

        EditorGUI.ObjectField(rect, serializedProperty.GetArrayElementAtIndex(index));
        if (EditorGUI.EndChangeCheck()) {
            OnChanged(list);
        }
    }

    protected virtual void OnAdd(ReorderableList list) {
        serializedProperty.arraySize++;
        serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1).SetDefault();
        serializedObj.ApplyModifiedPropertiesWithoutUndo();
    }

    protected virtual void OnRemove(ReorderableList list) {
        serializedProperty.DeleteArrayElementAtIndex(list.index);
        serializedObj.ApplyModifiedPropertiesWithoutUndo();
    }
    
    protected virtual void OnChanged(ReorderableList list) {
        var index = list.index;
        if (Value.IsValidIndex(index)) {
            if (Value[index].Equals(serializedProperty.GetArrayElementAtIndex(list.index).GetValue()) == false) {
                serializedObj.ApplyModifiedProperties();
                OnChanged(Value[list.index], list.index);
            }
        }
    }

    protected abstract void OnChanged(T value, int index);
}