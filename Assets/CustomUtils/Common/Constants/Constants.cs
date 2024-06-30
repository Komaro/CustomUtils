using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static partial class Constants {

    public static class Extension {

        public const string JSON = ".json";
        public const string MANIFEST = ".manifest";
        public const string DLL = ".dll";
        public const string PDB = ".pdb";

        public const string JSON_FILTER = "*.json";
    }

    public static class Culture {
        
        public static readonly CultureInfo DEFAULT_CULTURE_INFO = new CultureInfo("en-US");
    }

    public static class Resource {

        public const string RESOURCES_FOLDER = "Resources";
        public const string RESOURCE_LIST = "ResourceList";
        
        public const string RESOURCE_LIST_JSON = "ResourceList.json";
    }

    public static class Network {

        public const string DEFAULT_LOCAL_HOST = "http://localhost:8000/";
    }

    public static class Path {
        
        public static readonly string PROJECT_FOLDER = Directory.GetParent(Application.dataPath)?.FullName;
        public static readonly string COMMON_CONFIG_FOLDER = $"{PROJECT_FOLDER}/Config";
        public static readonly string PROJECT_TEMP_FOLDER = $"{PROJECT_FOLDER}/Temp";
        public static readonly string PLUGINS_FOLDER = $"{Application.dataPath}/Plugins";
    }

    public static class Text {

        public static readonly Regex FOLDER_PATH_REGEX = new Regex(@".*[\\/]$");
        public static readonly Regex FILE_PATH_REGEX = new Regex(@".*\.[^\\/]+$");

        public const string FOLDER_CONTAINS_FORMAT = @".*[\\/](?i){0}(?-i)[\\/].*|.*[\\/](?i){0}(?-i).*";
    }
}
