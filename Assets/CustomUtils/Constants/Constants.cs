using System.Globalization;
using System.IO;
using UnityEngine;

public static partial class Constants {

    public static class Culture {
        
        public static readonly CultureInfo DEFAULT_CULTURE_INFO = new CultureInfo("en-US");
    }

    public static class Resource {

        public const string RESOURCES_FOLDER = "Resources";
        public const string RESOURCE_LIST_JSON = "ResourceList";
    }

    public static class Network {

        public const string DEFAULT_LOCAL_HOST = "http://localhost:80000/";
    }
    
    public static class Editor {

        public static readonly string COMMON_CONFIG_FOLDER = $"{Directory.GetParent(Application.dataPath)?.FullName}/Config";
        public static readonly string PROJECT_FOLDER = Directory.GetParent(Application.dataPath)?.FullName;
        
        public static readonly GUIStyle DIVIDE_STYLE = new();
        public static readonly GUIStyle PATH_STYLE = new();
        public static readonly GUIStyle BOLD_STYLE = new();
        public static readonly GUIStyle WHITE_BOLD_STYLE = new();
        public static readonly GUIStyle FIELD_TITLE_STYLE = new();
        
        public static readonly GUILayoutOption DEFAULT_LAYOUT = GUILayout.Width(300f);

        
        static Editor() {
            DIVIDE_STYLE.alignment = TextAnchor.MiddleCenter;
            DIVIDE_STYLE.normal.textColor = Color.cyan;
        
            PATH_STYLE.normal.textColor = Color.white;
            PATH_STYLE.fontStyle = FontStyle.Bold;
            PATH_STYLE.fontSize = 12;
        
            BOLD_STYLE.normal.textColor = Color.gray;
            BOLD_STYLE.fontStyle = FontStyle.Bold;
            
            WHITE_BOLD_STYLE.normal.textColor = Color.white;
            WHITE_BOLD_STYLE.fontStyle = FontStyle.Bold;

            FIELD_TITLE_STYLE.alignment = TextAnchor.MiddleCenter;
            FIELD_TITLE_STYLE.normal.textColor = Color.white;
            FIELD_TITLE_STYLE.fontStyle = FontStyle.Bold;
            FIELD_TITLE_STYLE.padding = new RectOffset(2, 2, 2, 2);
        }
    }
}
