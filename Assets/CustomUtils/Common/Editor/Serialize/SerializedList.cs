using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public abstract class SerializedList<T> : SerializedObject<List<T>> {
    protected ReorderableList list;

    protected override void OnEnable() {
        if (isInitialized) {
            return;
        }

        serializedObj = new SerializedObject(this);
        serializedProperty = serializedObj.FindProperty(nameof(Value));

        list = new ReorderableList(serializedObj, serializedProperty, false, false, true, true);
        list.onAddCallback += OnAdd;
        list.onRemoveCallback += OnRemove;
        list.drawElementCallback += OnElementDraw;
        list.elementHeightCallback += OnElementHeight;

        isInitialized = true;
    }

    public void Init(List<T> value) {
        Value = value;
        serializedObj.Update();
    }

    public override void Draw() {
        if (isInitialized) {
            list.DoLayoutList();
        }
    }

    protected virtual void OnElementDraw(Rect rect, int index, bool isActive, bool isFocused) {
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(rect, serializedProperty.GetArrayElementAtIndex(index), true);

        if (EditorGUI.EndChangeCheck()) {
            serializedObj.ApplyModifiedProperties();
            OnChanged(Value[index], index);
        }
    }

    protected virtual void OnAdd(ReorderableList list) {
        serializedProperty.arraySize++;
        serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1).SetDefault();
        serializedObj.ApplyModifiedProperties();
        OnAdded(Value[^1], Value.Count - 1);
    }

    protected virtual void OnRemove(ReorderableList list) {
        var index = list.index;
        serializedProperty.DeleteArrayElementAtIndex(index);
        serializedObj.ApplyModifiedProperties();
        OnRemoved(index);
    }

    protected virtual float OnElementHeight(int index) => EditorGUI.GetPropertyHeight(serializedProperty.GetArrayElementAtIndex(index), GUIContent.none, true);

    protected virtual void OnChanged(T value, int index) { }
    protected virtual void OnAdded(T value, int index) { }
    protected virtual void OnRemoved(int index) { }
}