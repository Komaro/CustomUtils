using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CustomPropertyDrawer(typeof(BoolCheckFoldAttribute))]
public class BoolCheckFoldDrawerBase : PropertyDrawer {
    
    private string _fieldName;
    private object _targetObject;
    private Dictionary<string, FieldInfo> _fieldInfoDic = new();
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (attribute is BoolCheckFoldAttribute foldAttribute) {
            if (string.IsNullOrEmpty(foldAttribute.targetFieldName) == false && _targetObject == null) {
                _targetObject = GetTargetObject(property);
                if (_targetObject != null) {
                    _fieldName = fieldInfo.TryGetCustomAttribute<FormerlySerializedAsAttribute>(out var formerlyAttribute) ? formerlyAttribute.oldName : fieldInfo.Name;
                    if (_targetObject != null) {
                        var configType = _targetObject.GetType();
                        while (configType.BaseType != null) {
                            foreach (var value in configType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                                if (value.TryGetCustomAttribute(out formerlyAttribute)) {
                                    _fieldInfoDic.AutoAdd(formerlyAttribute.oldName, value);
                                }
                                
                                _fieldInfoDic.AutoAdd(value.Name, value);
                            }

                            configType = configType.BaseType;
                        }
                    }
                }
            }

            if (_fieldInfoDic.TryGetValue(foldAttribute.targetFieldName, out var info) && info.GetValue(_targetObject) is true) {
                EditorGUI.PropertyField(position, property, new GUIContent(_fieldName));
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (_targetObject != null && attribute is BoolCheckFoldAttribute foldAttribute && _fieldInfoDic.TryGetValue(foldAttribute.targetFieldName, out var info) && info.GetValue(_targetObject) is true) {
            return base.GetPropertyHeight(property, label);
        }

        return 0f;
    }

    protected virtual object GetTargetObject(SerializedProperty property) => property.serializedObject.targetObject;
}
