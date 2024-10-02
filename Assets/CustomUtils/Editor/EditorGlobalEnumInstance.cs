using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GlobalEnum<>))]
public class EditorGlobalEnumInstance : PropertyDrawer {
    
    private int _index;
    private string[] _enumStrings;
    
    private bool _isInitialize;

    private void Init(SerializedProperty property) {
        var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
        if (obj != null) {
            if (obj.GetType().TryGetField(out var info, "_index", BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (info.GetValue(obj) is int index) {
                    _index = index;
                }
            }
            
            if (obj.GetType().TryGetField(out info, "_intToEnumDic", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                if (info.GetValue(obj) is ImmutableDictionary<int, Enum> intToEnumDic) {
                    _enumStrings = intToEnumDic.Values.Select(enumValue => enumValue.GetType().GetAlias($"{enumValue.GetType()}.{enumValue}")).ToArray();
                }
            }
            
            _isInitialize = true;
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (_isInitialize == false) {
            Init(property);
        }

        using (var scope = new EditorGUI.ChangeCheckScope()) {
            _index = EditorGUI.Popup(position, label.text, _index, _enumStrings);
            if (scope.changed) {
                SetIndex(property, _index);
            }
        }
    }

    public void SetIndex(SerializedProperty property, int index) {
        var obj = fieldInfo.GetValue(property.serializedObject.targetObject); 
        if (obj != null && obj.GetType().TryGetField(out var info, "_index", BindingFlags.NonPublic | BindingFlags.Instance)) {
            info.SetValue(obj, index);
        }
    }
}
