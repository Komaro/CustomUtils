using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using SystemPath = System.IO.Path;
using SystemRegex = System.Text.RegularExpressions.Regex;

public static partial class Constants {

    public static class Extension {

        public const string JSON = ".json";
        public const string MANIFEST = ".manifest";
        public const string DLL = ".dll";
        public const string PDB = ".pdb";
        public const string ZIP = ".zip";
        public const string XML = ".xml";
        public const string CSV = ".csv";
        public const string EXE = ".exe";

        public const string JSON_FILTER = "*.json";
        public const string SOLUTION_FILTER = "*.sln";
        public const string TEST_CASE_FILTER = "*.tc";
        public const string DLL_FILTER = "*.dll";
    }

    public static class Culture {
        
        public static readonly CultureInfo DEFAULT_CULTURE_INFO = new CultureInfo("en-US");
    }

    public static class Resource {

        public const string RESOURCE_LIST = "ResourceList";
        
        public const string RESOURCE_LIST_JSON = "ResourceList.json";
    }

    public static class Network {

        public const string DEFAULT_LOCAL_HOST = "http://localhost:8000/";
    }

    public static class File {

        public static readonly string SOLUTION;
        
        static File() => SOLUTION = Path.SOLUTION_PATH != default ? SystemPath.GetFileName(Path.SOLUTION_PATH) : string.Empty;
    }

    public static class Path {
        public static readonly string PROJECT_PATH = Directory.GetParent(Application.dataPath)?.FullName;
        public static readonly string SOLUTION_PATH = Directory.GetFiles(PROJECT_PATH, Extension.SOLUTION_FILTER).FirstOrDefault();
        
        public static readonly string COMMON_CONFIG_PATH = $"{PROJECT_PATH}/{Folder.CONFIG}";
        public static readonly string PROJECT_TEMP_PATH = $"{PROJECT_PATH}/{Folder.TEMP}";
        
        public static readonly string PLUGINS_FULL_PATH = $"{Application.dataPath}/{Folder.PLUGINS}";
        public static readonly string PLUGINS_PATH = $"{Folder.ASSETS}/{Folder.PLUGINS}";

        public static readonly string RESOURCES_FULL_PATH = $"{Application.dataPath}/{Folder.RESOURCES}";
        public static readonly string RESOURCES_PATH = $"{Folder.ASSETS}/{Folder.RESOURCES}";

        public static readonly string BUILD_ROOT_PATH = $"{PROJECT_PATH}/{Folder.BUILD}";

        public static readonly string MEMORY_CAPTURES_PATH = $"{PROJECT_PATH}/{Folder.MEMORY_CAPTURES}";
    }

    public static class Folder {

        public const string ASSETS = "Assets";
        public const string CONFIG = "Config";
        public const string TEMP = "Temp";
        public const string SCRIPT_ASSEMBLIES = "ScriptAssemblies";
        public const string PLUGINS = "Plugins";
        public const string RESOURCES = "Resources";
        public const string BUILD = "Build";
        public const string MEMORY_CAPTURES = "MemoryCaptures";
    }

    public static class Regex {

        public static readonly SystemRegex FOLDER_PATH_REGEX = new(@".*[\\/]$");
        public static readonly SystemRegex FILE_PATH_REGEX = new(@".*\.[^\\/]+$");
        public static readonly SystemRegex UPPER_UNICODE_REGEX = new(@"(\p{Lu}})");
        
        public const string FOLDER_CONTAINS_FORMAT = @".*[\\/](?i){0}(?-i)[\\/].*|.*[\\/](?i){0}(?-i).*";
        public const string GET_AFTER_REGEX = @"({0}.*)";
    }

    public static class Colors {
        
    }

    public static class Separator {

        public const char BUILD_ARGUMENT = ':';
        public const char DEFINE_SYMBOL = ';';
    }
}
