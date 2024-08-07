﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

public static class AssetDatabaseUtil {

    private static readonly Regex ASSETS_GET_AFTER_REGEX = new(string.Format(Constants.Regex.GET_AFTER_REGEX, @"Assets[\\/]"));

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

    public static string GetCollectionPath(string path) => ASSETS_GET_AFTER_REGEX.TryMatch(path, out var match) ? match.Value : path;
    
    public static bool ContainsLabel(string path, string label) => TryGetLabels(path, out var labels) && labels.Contains(label);
}
