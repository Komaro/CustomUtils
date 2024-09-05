using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

public static class AssetDatabaseUtil {

    private static readonly Regex ASSETS_GET_AFTER_REGEX = new(string.Format(Constants.Regex.GET_AFTER_REGEX, @"Assets[\\/]"));

    public static bool TryFindAssets<T>(out IEnumerable<T> assets, string filter, bool ignorePackages = true) where T : Object => (assets = FindAssets<T>(filter, ignorePackages)).Any();
    
    public static IEnumerable<T> FindAssets<T>(string filter, bool ignorePackages = true) where T : Object => ignorePackages 
        ? AssetDatabase.FindAssets(filter).Where(guid => guid.StartsWith(Constants.Folder.ASSETS)).Select(LoadAssetFromGuid<T>) 
        : AssetDatabase.FindAssets(filter).Select(LoadAssetFromGuid<T>);

    public static bool TryFindAssetInfos<T>(out IEnumerable<EditorAssetInfo<T>> infos, string filter, bool ignorePackage = true) where T : Object => (infos = FindAssetInfos<T>(filter, ignorePackage)).Any(); 
    
    public static IEnumerable<EditorAssetInfo<T>> FindAssetInfos<T>(string filter, bool ignorePackage = true) where T : Object {
        foreach (var guid in AssetDatabase.FindAssets(filter)) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (ignorePackage && path.StartsWith(Constants.Folder.ASSETS) == false) {
                continue;
            }

            yield return new EditorAssetInfo<T>(guid, path);
        }
    }

    public static T LoadAssetFromGuid<T>(string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

    public static bool TryLoad(string path, out DefaultAsset asset) {
        asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path) ?? AssetDatabase.LoadAssetAtPath<DefaultAsset>(GetCollectionPath(path));
        return asset != null;
    }

    public static bool TryLoad<T>(string path, out T asset) where T : Object {
        asset = AssetDatabase.LoadAssetAtPath<T>(path) ?? AssetDatabase.LoadAssetAtPath<T>(GetCollectionPath(path));
        return asset != null;
    }

    public static bool TryGetLabels(string path, out string[] labels) {
        var guid = AssetDatabase.GUIDFromAssetPath(path);
        if (guid.Empty()) {
            guid = AssetDatabase.GUIDFromAssetPath(GetCollectionPath(path));
        }

        if (guid.Empty() == false) {
            labels = AssetDatabase.GetLabels(guid);
            return true;
        }

        labels = Array.Empty<string>();
        return false;
    }

    public static bool TrySetLabels(string path, params string[] labels) {
        if (TryLoad(path, out var asset)) {
            AssetDatabase.SetLabels(asset, labels);
            return true;
        }
        
        return false;
    }

    public static bool TryClearLabels(string path) {
        if (TryLoad(path, out var asset)) {
            AssetDatabase.ClearLabels(asset);
            return true;
        }

        return false;
    }

    public static IEnumerable<string> GetAllPluginPaths() => AssetDatabase.FindAssets("t:DefaultAsset", new [] { Constants.Path.PLUGINS_PATH })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => string.IsNullOrEmpty(path) == false && path.ContainsExtension(Constants.Extension.DLL));

    public static string GetCollectionPath(string path) => ASSETS_GET_AFTER_REGEX.TryMatch(path, out var match) ? match.Value : path;
    
    public static bool ContainsLabel(string path, string label) => TryGetLabels(path, out var labels) && labels.Contains(label);
}

public record EditorAssetInfo<T> where T : Object {

    public readonly T asset;
    public readonly string guid;
    public readonly string path;

    public string Name => asset != null ? asset.name : Path.GetFileName(path);

    public EditorAssetInfo(T asset, string guid, string path) {
        this.asset = asset;
        this.guid = guid;
        this.path = path;
    }

    public EditorAssetInfo(string guid, string path) {
        asset = AssetDatabase.LoadAssetAtPath<T>(path);
        this.guid = guid;
        this.path = path;
    }

    public EditorAssetInfo(string path) {
        asset = AssetDatabase.LoadAssetAtPath<T>(path);
        guid = AssetDatabase.GUIDFromAssetPath(path).ToString();
        this.path = path;
    }
}
