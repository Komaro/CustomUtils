using System;
using System.Collections.Concurrent;
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
                if (obj.GetType().TryGetFieldInfo(out var info, "_index", BindingFlags.NonPublic | BindingFlags.Instance)) {
                    if (info.GetValue(obj) is int index) {
                        _index = index;
                    }
                }
            
                foreach (var baseType in obj.GetType().GetBaseTypes()) {
                    if (baseType == typeof(GlobalEnum) && baseType.TryGetFieldInfo(out info, "intToEnumDic", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                        if (info.GetValue(obj) is ConcurrentDictionary<Type, ImmutableDictionary<int, Enum>> intToEnumDic) {
                            _enumStrings = intToEnumDic.Values.SelectMany(pair => pair.Values).Select(enumValue => enumValue.GetType().GetAlias($"{enumValue.GetType()}.{enumValue}")).ToArray();
                        }
                    
                        break;
                    }
                }

                _isInitialize = _enumStrings != null && _enumStrings.Any();
            }
        } catch (Exception ex) {
            _isInitialize = false;
            _exception = ex;
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (_isInitialize == false) {
            Init(property);
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
        if (obj != null && obj.GetType().TryGetFieldInfo(out var info, "_index", BindingFlags.NonPublic | BindingFlags.Instance)) {
            info.SetValue(obj, index);
        }
    }
}
