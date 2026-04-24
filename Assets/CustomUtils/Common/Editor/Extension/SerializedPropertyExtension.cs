using UnityEditor;
using UnityEngine;

public static class SerializedPropertyExtension {
    
    public static object GetValue(this SerializedProperty property) => property.propertyType switch {
        SerializedPropertyType.Integer => property.intValue,
        SerializedPropertyType.Boolean => property.boolValue,
        SerializedPropertyType.Float => property.floatValue,
        SerializedPropertyType.String => property.stringValue,
        SerializedPropertyType.Color => property.colorValue,
        SerializedPropertyType.ObjectReference => property.objectReferenceValue,
        SerializedPropertyType.LayerMask => property.intValue,
        SerializedPropertyType.Enum => property.enumValueIndex,
        SerializedPropertyType.Vector2 => property.vector2Value,
        SerializedPropertyType.Vector3 => property.vector3Value,
        SerializedPropertyType.Vector4 => property.vector4Value,
        SerializedPropertyType.Rect => property.rectValue,
        SerializedPropertyType.ArraySize => property.intValue,
        SerializedPropertyType.Character => (char)property.intValue,
        SerializedPropertyType.AnimationCurve => property.animationCurveValue,
        SerializedPropertyType.Bounds => property.boundsValue,
        SerializedPropertyType.Quaternion => property.quaternionValue,
        SerializedPropertyType.ExposedReference => property.exposedReferenceValue,
        SerializedPropertyType.Vector2Int => property.vector2IntValue,
        SerializedPropertyType.Vector3Int => property.vector3IntValue,
        SerializedPropertyType.RectInt => property.rectIntValue,
        SerializedPropertyType.BoundsInt => property.boundsIntValue,
        SerializedPropertyType.ManagedReference => property.managedReferenceValue,
        SerializedPropertyType.Hash128 => property.hash128Value,
        _ => null
    };

    public static void SetValue<T>(this SerializedProperty property, T value) {
        switch (property.propertyType) {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.ArraySize:
                property.intValue = value is int intValue ? intValue : 0;
                break;
            case SerializedPropertyType.Boolean:
                property.boolValue = value is bool boolValue && boolValue;
                break;
            case SerializedPropertyType.Float:
                property.floatValue = value is float floatValue ? floatValue : 0;
                break;
            case SerializedPropertyType.String:
                property.stringValue = value as string ?? string.Empty;
                break;
            case SerializedPropertyType.Color:
                property.colorValue = value is Color colorValue ? colorValue : default;
                break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = value as Object;
                break;
            case SerializedPropertyType.Enum:
                property.enumValueIndex = value is int enumIndex ? enumIndex : 0;
                break;
            case SerializedPropertyType.Vector2:
                property.vector2Value = value is Vector2 vector2Value ? vector2Value : default;
                break;
            case SerializedPropertyType.Vector3:
                property.vector3Value = value is Vector3 vector3Value ? vector3Value : default;
                break;
            case SerializedPropertyType.Vector4:
                property.vector4Value = value is Vector4 vector4Value ? vector4Value : default;
                break;
            case SerializedPropertyType.Rect:
                property.rectValue = value is Rect rectValue ? rectValue : default;
                break;
            case SerializedPropertyType.Character:
                property.intValue = value is char charValue ? charValue : '\0';
                break;
            case SerializedPropertyType.AnimationCurve:
                property.animationCurveValue = value as AnimationCurve;
                break;
            case SerializedPropertyType.Bounds:
                property.boundsValue = value is Bounds boundsValue ? boundsValue : default;
                break;
            case SerializedPropertyType.Quaternion:
                property.quaternionValue = value is Quaternion quaternionValue ? quaternionValue : default;
                break;
            case SerializedPropertyType.ExposedReference:
                property.exposedReferenceValue = value as Object;
                break;
            case SerializedPropertyType.Vector2Int:
                property.vector2IntValue = value is Vector2Int vector2IntValue ? vector2IntValue : default;
                break;
            case SerializedPropertyType.Vector3Int:
                property.vector3IntValue = value is Vector3Int vector3IntValue ? vector3IntValue : default;
                break;
            case SerializedPropertyType.RectInt:
                property.rectIntValue = value is RectInt rectIntValue ? rectIntValue : default;
                break;
            case SerializedPropertyType.BoundsInt:
                property.boundsIntValue = value is BoundsInt boundsIntValue ? boundsIntValue : default;
                break;
            case SerializedPropertyType.ManagedReference:
                property.managedReferenceValue = value;
                break;
            case SerializedPropertyType.Hash128:
                property.hash128Value = value is Hash128 hash128Value ? hash128Value : default;
                break;
        }
    }

    public static void SetDefault(this SerializedProperty property) => property.SetValue<object>(null);
}