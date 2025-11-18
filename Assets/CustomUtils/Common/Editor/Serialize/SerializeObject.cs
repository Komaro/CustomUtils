using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject를 인스턴스화 할 때 Generic 타입이 있는 경우 인스턴스화를 할 수 없기 때문에 각각의 상요처마다 NonGenric 타입을 구현하여서 사용하여야 함.
/// </summary>
/// <typeparam name="T"></typeparam>
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