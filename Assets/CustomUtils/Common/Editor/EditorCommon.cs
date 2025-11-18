using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class EditorCommon {
    
    [MenuItem("GameObject/Tool/Copy Path", false, 99)]
    public static void CopyPath() {
        var go = Selection.activeGameObject;
        if (go == null) {
            Logger.TraceError($"{nameof(go)} is Null");
            return;
        }

        var path = go.name;
        while (go.transform.parent != null) {
            go = go.transform.parent.gameObject;
            path = $"{go.name}/{path}";
        }

        EditorGUIUtility.systemCopyBuffer = path;
    }

    [MenuItem("GameObject/Tool/Copy Path", true, 99)]
    public static bool CopyPathValidation() => Selection.gameObjects.Length == 1;

    #region [IO]
    
    public static List<string> SearchFilePath(string directoryPath, string fileName) {
        if (Directory.Exists(directoryPath)) {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var fileInfos = directoryInfo.GetFiles(fileName, SearchOption.AllDirectories);
            if (fileInfos.Length > 0) {
                fileInfos.ForEach(x => Debug.Log(x.FullName));
            }

            return fileInfos.ToList(x => x.FullName);
        }
        
        return null;
    }
    
    public static bool TryLoadText(string path, out string text) {
        text = LoadText(path);
        return string.IsNullOrEmpty(text) == false;
    }
    
    public static string LoadText(string path) {
        try {
            if (File.Exists(path) == false) {
                Debug.LogError($"Invalid Path || {path}");
                throw new FileNotFoundException();
            }

            return File.ReadAllText(path);
        } catch (Exception e) {
            Debug.LogError(e);
            return string.Empty;
        }
    }

    public static void SaveText(string path, string text) {
        try {
            if (string.IsNullOrEmpty(path)) {
                Debug.LogError($"{nameof(path)} is Null or Empty");
                return;
            }
            
            var parentPath = Directory.GetParent(path)?.FullName;
            if (string.IsNullOrEmpty(parentPath)) {
                Debug.LogError($"{nameof(parentPath)} is Null or Empty");
                return;
            }
            
            if (Directory.Exists(parentPath) == false) {
                Directory.CreateDirectory(parentPath);
            }
            
            File.WriteAllText(path, text);
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }

    #endregion
    
    #region [Editor PlayerPrefs]
    
    public static bool TryGet(string key, out string value) => EditorPrefsUtil.TryGet($"EditorString_{key}", out value); 
    public static bool TryGet(string key, out bool value) => EditorPrefsUtil.TryGet($"EditorBool_{key}", out value);
    public static bool TryGet(string key, out int value) => EditorPrefsUtil.TryGet($"EditorInt_{key}", out value);
    public static bool TryGet(string key, out float value) => EditorPrefsUtil.TryGet($"EditorFloat_{key}", out value);
    public static bool TryGet<TEnum>(string key, out TEnum value) where TEnum : struct, Enum => EditorPrefsUtil.TryGet($"EditorEnum_{key}", out value);

    public static void Set(string key, string value) => EditorPrefsUtil.SetString($"EditorString_{key}", value);
    public static void Set(string key, bool value) => EditorPrefsUtil.Set($"EditorBool_{key}", value);
    public static void Set(string key, int value) => EditorPrefsUtil.SetInt($"EditorInt_{key}", value);
    public static void Set(string key, float value) => EditorPrefsUtil.SetFloat($"EditorFloat_{key}", value);
    public static void Set<TEnum>(string key, TEnum value) where TEnum : struct, Enum => EditorPrefsUtil.Set($"EditorEnum_{key}", value);

    #endregion

    #region [Editor SessionState]

    public static bool TryGetSession(string key, out string value) => SessionStateUtil.TryGet($"EditorSessionString_{key}", out value);
    public static bool TryGetSession(string key, out bool value) => SessionStateUtil.TryGet($"EditorSessionBool_{key}", out value);
    public static bool TryGetSession(string key, out int value) => SessionStateUtil.TryGet($"EditorSessionInt_{key}", out value);
    public static bool TryGetSession(string key, out float value) => SessionStateUtil.TryGet($"EditorSessionFloat_{key}", out value);

    public static void SetSession(string key, string value) => SessionStateUtil.Set($"EditorSessionString_{key}", value);
    public static void SetSession(string key, bool value) => SessionStateUtil.Set($"EditorSessionBool_{key}", value);
    public static void SetSession(string key, int value) => SessionStateUtil.Set($"EditorSessionInt_{key}", value);
    public static void SetSession(string key, float value) => SessionStateUtil.Set($"EditorSessionFloat_{key}", value);

    #endregion

    public static void LookUp(string assetPath) {
        assetPath.ThrowIfNull(nameof(assetPath));
        if (AssetDatabaseUtil.TryLoad(assetPath, out var asset)) {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
