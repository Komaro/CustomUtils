using UnityEditor;
using UnityEngine;

public static class SerializedPropertyExtension {

    public static object GetValue(this SerializedProperty property) => property.propertyType == SerializedPropertyType.ManagedReference ? property.managedReferenceValue : property.objectReferenceValue;

    public static void SetValue<T>(this SerializedProperty property, T value) {
        if (property.propertyType == SerializedPropertyType.ManagedReference) {
            property.managedReferenceValue = value;
        } else {
            property.objectReferenceValue = value as Object;
        }
    }

    public static void SetDefault(this SerializedProperty property) {
        if (property.propertyType == SerializedPropertyType.ManagedReference) {
            property.managedReferenceValue = default;
        } else {
            property.objectReferenceValue = null;
        }
    }
}