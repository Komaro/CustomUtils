using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UnitySoundTrackEvent))]
public class EditorUnitySoundTrackEventInstance : PropertyDrawer {
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (EditorGUIUtility.singleLineHeight + 2) * 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(UnitySoundTrackEvent.clip)), GUIContent.none);

        var yPosition = position.y + EditorGUIUtility.singleLineHeight + 2;
        var width = position.width / 2;
        EditorGUI.PropertyField(new Rect(position.x, yPosition, width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(UnitySoundTrackEvent.loop)));
        EditorGUI.PropertyField(new Rect(position.x + width, yPosition, width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(UnitySoundTrackEvent.startTime)));

        EditorGUI.EndProperty();
    }
}