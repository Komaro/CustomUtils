using System;
using UnityEditor;
using UnityEngine;

[TestRequired]
[Serializable]
public abstract class SerializedObject<T> : ScriptableObject {

    [SerializeField]
    public T Value;

    protected SerializedObject serializedObj;
    protected SerializedProperty serializedProperty;

    protected bool isInitialized;
    
    protected virtual void OnEnable() {
        serializedObj ??= new SerializedObject(this);
        serializedProperty ??= serializedObj.FindProperty(nameof(Value));
        isInitialized = serializedProperty != null;
    }

    public virtual void Draw() {
        if (isInitialized) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedProperty);

            if (EditorGUI.EndChangeCheck()) {
                OnChangeValue();
            }
        } else {
            EditorCommon.DrawFitLabel("초기화중...");
        }
    }

    protected virtual void OnChangeValue() { }
}