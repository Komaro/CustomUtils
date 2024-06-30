using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AssetDatabaseUtil {

    public static bool TryLoad(string path, out DefaultAsset asset) {
        asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        return asset != null;
    }

    public static bool TryLoad<T>(string path, out T asset) where T : Object {
        asset = AssetDatabase.LoadAssetAtPath<T>(path);
        return asset != null;
    }

    public static string GetCollectionPath(string path) => path.StartsWith("Assets/") ? path : path.GetAfter("Assets/", true);
}
