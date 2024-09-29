using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoundTrackEvent))]
public class EditorSoundTrackEventInstance : PropertyDrawer {

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (EditorGUIUtility.singleLineHeight + 2) * 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(SoundTrackEvent.clip)), GUIContent.none);

        var yPosition = position.y + EditorGUIUtility.singleLineHeight + 2;
        var width = position.width / 2;
        EditorGUI.PropertyField(new Rect(position.x, yPosition, width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(SoundTrackEvent.loop)));
        EditorGUI.PropertyField(new Rect(position.x + width, yPosition, width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(SoundTrackEvent.startTime)));

        EditorGUI.EndProperty();
    }
}
