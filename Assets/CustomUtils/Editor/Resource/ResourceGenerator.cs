using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

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
                onProgress?.Invoke($"Generate {Constants.Resource.RESOURCE_LIST_JSON}.json", nameKey, progress);
                if (jObject.ContainsKey(nameKey)) {
                    Logger.TraceError($"Duplicate Resource {nameof(nameKey)}\nPath|| {path}\nPath || {jObject[nameKey]}");
                    continue;
                }

                jObject.AutoAdd(nameKey, path.Remove(0, RESOURCES_START_PATH.Length).Split('.')[0]);
            }

            if (jObject.ContainsKey(Constants.Resource.RESOURCE_LIST_JSON.ToUpper()) == false) {
                jObject.AutoAdd(Constants.Resource.RESOURCE_LIST_JSON.ToUpper(), Constants.Resource.RESOURCE_LIST_JSON);
            }
            
            File.WriteAllText(generatePath, jObject.ToString());
            AssetDatabase.Refresh();
            
            onEnd?.Invoke();
        } catch (Exception e) {
            Logger.TraceError(e);
            throw;
        }
    }

    [MenuItem("Service/Test/GenerateAssetBundleListJson")]
    private static void TestGenerateAssetBundleListJson() => GenerateAssetBundleListJson(string.Empty);

    [MenuItem("Service/Test/GenerateAssetBundle")]
    private static void TestGenerateAssetBundle() => GenerateAssetBundle($"C:/Project/Unity/CustomUtils/AssetBundle/{EditorUserBuildSettings.activeBuildTarget}", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    
    // TODO. AssetBundle 빌드 시 manifest 파일로 대처 가능한지 검토
    public static void GenerateAssetBundleListJson(string generatePath, Action<string, string, float> onProgress = null, Action onEnd = null) {
        try {
            var jObject = new JObject();
            foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames().Where(Path.HasExtension)) {
                Logger.TraceError(bundleName);
                foreach (var assetName in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName)) {
                    Logger.TraceError(assetName);
                }
            }
        } catch (Exception e) {
            Logger.TraceError(e);
            throw;
        }
    }
    
    public static void GenerateAssetBundle(string generatePath, BuildAssetBundleOptions option, BuildTarget target) {
        try {
            if (Directory.Exists(generatePath) == false) {
                Directory.CreateDirectory(generatePath);
            }

            var manifest = BuildPipeline.BuildAssetBundles(generatePath, option, target);
            if (manifest != null) {
                var manifestPath = Path.Combine(generatePath, EditorUserBuildSettings.activeBuildTarget.ToString());
                if (File.Exists(manifestPath)) {
                    var manifestBytes = File.ReadAllBytes(manifestPath);
                    var aesManifestBytes = EncryptUtil.EncrytAES(manifestBytes);
                    // File.Copy(manifestPath, Path.Combine(generatePath, EditorUserBuildSettings.activeBuildTarget.ToString()));
                    File.WriteAllBytes(manifestPath, aesManifestBytes);
                }
            }
        } catch (Exception e) {
            Logger.TraceError(e);
            throw;
        }
    }
}