using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AssetDatabaseUtil {

    private static readonly Regex ASSETS_GET_AFTER_REGEX = new(string.Format(Constants.Regex.GET_AFTER_REGEX, @"Assets[\\/]"));

    public static bool TryGetAssetDirectory(Object obj, out string path) => string.IsNullOrEmpty(path = GetAssetDirectory(obj)) == false;

    public static string GetAssetDirectory(Object obj) {
        if (obj == null) {
            Logger.TraceLog($"{nameof(obj)} is null", Color.red);
            return string.Empty;
        }
        
        var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj));
        return AssetDatabase.IsValidFolder(path) ? path : string.Empty;
    }
    
    public static bool TryFindAssets<T>(out IEnumerable<T> assets, string filter) where T : Object => (assets = FindAssets<T>(filter)).Any();
    
    public static IEnumerable<T> FindAssets<T>(string filter) where T : Object {
        foreach (var guid in AssetDatabase.FindAssets(filter)) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<T>(path);
            if (obj == null) {
                continue;
            }

            yield return obj;
        }
    }

    public static bool TryFindAssetInfos<T>(out IEnumerable<EditorAssetInfo<T>> infos, string filter) where T : Object => (infos = FindAssetInfos<T>(filter)).Any(); 
    
    public static IEnumerable<EditorAssetInfo<T>> FindAssetInfos<T>(string filter) where T : Object {
        var guids = AssetDatabase.FindAssets(filter);
        if (guids.Length <= 0) {
            yield break;
        }
        
        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (TryLoad<T>(path, out var asset)) {
                yield return new EditorAssetInfo<T>(asset, guid, path);
            }
        }
    }

    public static T LoadAssetFromGuid<T>(string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

    public static bool TryLoad(string path, out Object asset) => (asset = AssetDatabase.LoadAssetAtPath<Object>(path) ?? AssetDatabase.LoadAssetAtPath<Object>(GetCollectionPath(path))) != null;
    public static bool TryLoad<T>(string path, out T asset) where T : Object => (asset = AssetDatabase.LoadAssetAtPath<T>(path) ?? AssetDatabase.LoadAssetAtPath<T>(GetCollectionPath(path))) != null;

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

public readonly struct EditorAssetInfo<T> where T : Object {

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
    
    public bool IsValid() => asset != null && string.IsNullOrEmpty(guid) == false && string.IsNullOrEmpty(path) == false;
}

public static class FilterUtil {

    public static string CreateFilter(TypeFilter type) => string.Intern(string.Concat("t:", type.ToString()));
    public static string CreateFilter(AreaFilter area) => string.Intern(string.Concat("a:", area.ToString()));
    public static string CreateFilter(TypeFilter type, AreaFilter area) => string.Intern(string.Concat(CreateFilter(type), " a:", area.ToString()));
}

public enum TypeFilter {
    Texture,
    AssemblyDefinitionAsset,
    ScriptableObject,
    Scene,
}

public enum AreaFilter {
    all,
    assets,
    packages,
}