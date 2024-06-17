#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static partial class Constants {
    
    public static class Editor {

        public static readonly GUIStyle LABEL = new (GUI.skin.label) { richText = true };
        public static readonly GUIStyle BOLD_LABEL = new (LABEL) { fontStyle = FontStyle.Bold };
        public static readonly GUIStyle TEXT_FIELD = new (GUI.skin.textField) { richText = true };
        public static readonly GUIStyle BUTTON = new(GUI.skin.button) { richText = true };
        public static readonly GUIStyle TOGGLE = new GUIStyle(GUI.skin.toggle) { richText = true };
    
        public static readonly GUIStyle BLACK_OR_WHITE_LABEL;
        
        public static readonly GUIStyle DIVIDE_STYLE;
        public static readonly GUIStyle PATH_STYLE;
        public static readonly GUIStyle TITLE_STYLE;
        public static readonly GUIStyle AREA_TITLE_STYLE;
    
        public static readonly GUILayoutOption DEFAULT_LAYOUT = GUILayout.Width(300f);
    
        static Editor() {
            BLACK_OR_WHITE_LABEL = new GUIStyle(LABEL) {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            DIVIDE_STYLE = new GUIStyle(LABEL) {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.cyan },
                fontSize = 12
            };
    
            PATH_STYLE = new GUIStyle(BOLD_LABEL) {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
    
            TITLE_STYLE = new GUIStyle(BLACK_OR_WHITE_LABEL) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
    
            AREA_TITLE_STYLE = new GUIStyle(BLACK_OR_WHITE_LABEL) {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

#endif
