using System;
using System.Collections.Generic;
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
    private Exception _exception;

    private void Init(SerializedProperty property) {
        try {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj != null) {
                if (obj.GetType().TryGetFieldInfo("_index", BindingFlags.NonPublic | BindingFlags.Instance, out var info) && info.GetValue(obj) is int index) {
                    _index = index;
                }
            
                foreach (var baseType in obj.GetType().GetBaseTypes()) {
                    if (baseType == typeof(GlobalEnum) && baseType.TryGetFieldInfo("intToEnumDic", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static, out info)) {
                        if (info.GetValue(obj) is Dictionary<Type, ImmutableDictionary<int, Enum>> intToEnumDic && intToEnumDic.TryGetValue(obj.GetType().GetGenericArguments().First(), out var enumDic)) {
                            _enumStrings = enumDic.Values.Select(enumValue => enumValue.GetType().GetAlias($"{enumValue.GetType()}.{enumValue}")).ToArray();
                            // _enumStrings = intToEnumDic.Values.SelectMany(pair => pair.Values).Select(enumValue => enumValue.GetType().GetAlias($"{enumValue.GetType()}.{enumValue}")).ToArray();
                        }
                    
                        break;
                    }
                }

                _isInitialize = _enumStrings != null && _enumStrings.Length > 0;
            }
        } catch (Exception ex) {
            _isInitialize = false;
            _exception = ex;
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (_isInitialize == false) {
            Init(property);
            return;
        }

        if (_exception != null) {
            EditorGUILayout.HelpBox(_exception.Message, MessageType.Error);
            return;
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
        if (obj != null && obj.GetType().TryGetFieldInfo("_index", BindingFlags.NonPublic | BindingFlags.Instance, out var info)) {
            info.SetValue(obj, index);
        }
    }
}
