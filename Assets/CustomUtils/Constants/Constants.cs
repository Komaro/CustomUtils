using System.Globalization;
using System.IO;
using UnityEngine;

public static partial class Constants {

    public static class Extension {

        public const string JSON = ".json";
        public const string MANIFEST = ".manifest";

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
        
        public static readonly string COMMON_CONFIG_FOLDER = $"{Directory.GetParent(Application.dataPath)?.FullName}/Config";
        public static readonly string PROJECT_FOLDER = Directory.GetParent(Application.dataPath)?.FullName;
    }
}
