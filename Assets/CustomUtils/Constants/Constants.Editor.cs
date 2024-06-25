#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static partial class Constants {
    
    public static class Editor {

        public static readonly GUIStyle LABEL = new (GUI.skin.label) { richText = true };
        public static readonly GUIStyle BOLD_LABEL = new (LABEL) { fontStyle = FontStyle.Bold };
        public static readonly GUIStyle CENTER_LABEL = new(LABEL) { alignment = TextAnchor.MiddleCenter };
        public static readonly GUIStyle TEXT_FIELD = new (GUI.skin.textField) { richText = true };
        public static readonly GUIStyle TEXT_AREA = new (GUI.skin.textArea) { richText = true };
        public static readonly GUIStyle BUTTON = new (GUI.skin.button) { richText = true };
        public static readonly GUIStyle TOGGLE = new (GUI.skin.toggle) { richText = true };
        public static readonly GUIStyle BOX = new (GUI.skin.box) { richText = true };
        
        public static readonly GUIStyle FIT_BUTTON = new (BUTTON) { padding = new RectOffset(0, 0, 0, 0)};
        public static readonly GUIStyle FIT_X2_BUTTON = new (BUTTON) { padding = new RectOffset(2, 2, 2, 2)};

        public static readonly GUIStyle TOGGLE_HORIZONTAL_SCOPE = new (GUIStyle.none) { margin = new RectOffset(0, 15, 0, 0) };
            
        public static readonly GUIStyle BLACK_OR_WHITE_LABEL;
        
        public static readonly GUIStyle DIVIDE_STYLE;
        public static readonly GUIStyle PATH_STYLE;
        public static readonly GUIStyle TITLE_STYLE;
        public static readonly GUIStyle AREA_TITLE_STYLE;
        
        public static readonly GUIContent SHORT_CUT_ICON = new(string.Empty, EditorGUIUtility.IconContent("d_Shortcut Icon").image, "바로가기");
        public static readonly GUIContent FOLDER_OPEN_ICON = new(string.Empty, EditorGUIUtility.IconContent("d_FolderOpened Icon").image, "바로가기");
        
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
