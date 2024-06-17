using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ResourceGenerator {

    private const string RESOURCES_START_PATH = "Assets/Resources/";
    private const string ASSET_BUNDLE_MANIFEST = "AssetBundle.manifest";
    private const string BACKUP_ASSET_BUNDLE_MANIFEST = "BackupAssetBundle.manifest";
    
    public static void GenerateResourcesListJson(string generatePath, Action<string, string, float> onProgress = null, Action onEnd = null) {
        try {
            var jObject = new JObject();
            var pathList = AssetDatabase.GetAllAssetPaths().Where(x => Directory.Exists(x) == false && x.StartsWith(RESOURCES_START_PATH)).ToList();
            var progress = 0f;
            var tick = 1f / pathList.Count;
            foreach (var path in pathList) {
                progress += tick;
                var nameKey = Path.GetFileNameWithoutExtension(path).ToUpper();
                onProgress?.Invoke($"Generate {Constants.Resource.RESOURCE_LIST}.json", nameKey, progress);
                if (jObject.ContainsKey(nameKey)) {
                    Logger.TraceError($"Duplicate Resource {nameof(nameKey)}\nPath|| {path}\nPath || {jObject[nameKey]}");
                    continue;
                }

                jObject.AutoAdd(nameKey, path.Remove(0, RESOURCES_START_PATH.Length).Split('.')[0]);
            }

            if (jObject.ContainsKey(Constants.Resource.RESOURCE_LIST.ToUpper()) == false) {
                jObject.AutoAdd(Constants.Resource.RESOURCE_LIST.ToUpper(), Constants.Resource.RESOURCE_LIST);
            }

            if (Path.HasExtension(generatePath) == false) {
                generatePath = $"{generatePath}.json";
            }
            
            File.WriteAllText(generatePath, jObject.ToString());
            AssetDatabase.Refresh();
            
            onEnd?.Invoke();
        } catch (Exception e) {
            Logger.TraceError(e);
            throw;
        }
    }

    [MenuItem("Service/Test/GenerateAssetBundle")]
    private static void TestGenerateAssetBundle() => GenerateAssetBundle($"C:/Project/Unity/CustomUtils/AssetBundle/{EditorUserBuildSettings.activeBuildTarget}", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

    public static bool TryGenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target, out AssetBundleManifest manifest) {
        manifest = GenerateAssetBundle(generatePath, options, target);
        return manifest != null;
    }
    
    public static AssetBundleManifest GenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target) {
        try {
            if (Directory.Exists(generatePath) == false) {
                Directory.CreateDirectory(generatePath);
            }
            
            return BuildPipeline.BuildAssetBundles(generatePath, options, target);
        } catch (Exception e) {
            Logger.TraceError(e);
        }
        
        return null;
    }
}