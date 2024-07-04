using System.Globalization;
using System.IO;
using UnityEngine;
using SystemPath = System.IO.Path;
using SystemRegex = System.Text.RegularExpressions.Regex;

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

    public static class Folder {

        public const string CONFIG = "Config";
        public const string TEMP = "Temp";
        public const string SCRIPT_ASSEMBLIES = "ScriptAssemblies";
        public const string PLUGINS = "Plugins";
    }

    public static class Path {
        
        public static readonly string PROJECT_FOLDER = Directory.GetParent(Application.dataPath)?.FullName;
        public static readonly string COMMON_CONFIG_FOLDER = $"{PROJECT_FOLDER}/{Folder.CONFIG}";
        public static readonly string PROJECT_TEMP_FOLDER = $"{PROJECT_FOLDER}/{Folder.TEMP}";
        public static readonly string PLUGINS_FOLDER = $"{Application.dataPath}/{Folder.PLUGINS}";
    }

    public static class Regex {

        public static readonly SystemRegex FOLDER_PATH_REGEX = new(@".*[\\/]$");
        public static readonly SystemRegex FILE_PATH_REGEX = new(@".*\.[^\\/]+$");
        public static readonly SystemRegex UPPER_UNICODE_REGEX = new(@"(\p{Lu}})");

        public const string FOLDER_CONTAINS_FORMAT = @".*[\\/](?i){0}(?-i)[\\/].*|.*[\\/](?i){0}(?-i).*";
    }

    public static class Analyzer {
        
        public const string ANALYZER_PLUGIN_NAME = "CustomUtilsAnalyzer";
        public const string ROSLYN_ANALYZER_LABEL = "RoslynAnalyzer";
        
        public static readonly string ANALYZER_PLUGIN_PATH;

        static Analyzer() => ANALYZER_PLUGIN_PATH = SystemPath.Combine(Path.PLUGINS_FOLDER, $"{ANALYZER_PLUGIN_NAME}{Extension.DLL}");
    }
    
    public static class Colors {
        
    }
}
