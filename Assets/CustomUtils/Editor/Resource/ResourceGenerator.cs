using System;
using System.Collections.Generic;
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

    public static bool TryGenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target, out AssetBundleManifest manifest) {
        manifest = GenerateAssetBundle(generatePath, options, target);
        return manifest != null;
    }
    
    public static AssetBundleManifest GenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target) {
        try {
            SystemUtil.EnsureDirectoryExists(generatePath);
            return BuildPipeline.BuildAssetBundles(generatePath, options, target);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return null;
    }

    public static bool TryGenerateAssetBundle(out AssetBundleManifest manifest, string generatePath, BuildAssetBundleOptions options, BuildTarget target, params string[] assetBundles) {
        manifest = GenerateAssetBundle(generatePath, options, target, assetBundles);
        return manifest != null;
    }
    
    public static bool TryGenerateAssetBundle(out AssetBundleManifest manifest, string generatePath, BuildAssetBundleOptions options, BuildTarget target, params AssetBundleBuild[] assetBundles) {
        manifest = GenerateAssetBundle(generatePath, options, target, assetBundles);
        return manifest != null;
    }
    
    public static AssetBundleManifest GenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target, params string[] assetBundles) {
        var buildList = new List<AssetBundleBuild>();
        foreach (var name in assetBundles) {
            var info = new AssetBundleBuild {
                assetBundleName = name,
                assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(name)
            };

            buildList.Add(info);
        }

        return GenerateAssetBundle(generatePath, options, target, buildList.ToArray());
    }

    public static AssetBundleManifest GenerateAssetBundle(string generatePath, BuildAssetBundleOptions options, BuildTarget target, params AssetBundleBuild[] assetBundles) {
        try {
            SystemUtil.EnsureDirectoryExists(generatePath);
            return BuildPipeline.BuildAssetBundles(generatePath, assetBundles, options, target);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }
}