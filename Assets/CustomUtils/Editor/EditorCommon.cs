using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public static partial class EditorCommon {
    
    private static string _unityProjectPath;
    public static string UNITY_PROJECT_PATH => string.IsNullOrEmpty(_unityProjectPath) ? _unityProjectPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityProjectPath;

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

    public static List<string> SearchFilePath(string directoryPath, string fileName) {
        if (Directory.Exists(directoryPath)) {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var fileInfos = directoryInfo.GetFiles(fileName, SearchOption.AllDirectories);
            if (fileInfos.Length > 0) {
                fileInfos.ForEach(x => Debug.Log(x.FullName));
            }

            return fileInfos.ConvertTo(x => x.FullName).ToList();
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

    #region [Editor PlayerPrefs]
    
    public static bool TryGet(string key, out string value) => PlayerPrefsUtil.TryGet($"EditorString_{key}", out value); 
    public static bool TryGet(string key, out bool value) => PlayerPrefsUtil.TryGet($"EditorBool_{key}", out value);
    public static bool TryGet(string key, out int value) => PlayerPrefsUtil.TryGet($"EditorInt_{key}", out value);
    public static bool TryGet(string key, out float value) => PlayerPrefsUtil.TryGet($"EditorFloat_{key}", out value);
    public static bool TryGet<T>(string key, out T value) where T : struct, Enum => PlayerPrefsUtil.TryGet<T>($"EditorEnum_{key}", out value);

    public static void Set(string key, string value) => PlayerPrefsUtil.SetString($"EditorString_{key}", value);
    public static void Set(string key, bool value) => PlayerPrefsUtil.Set($"EditorBool_{key}", value);
    public static void Set(string key, int value) => PlayerPrefsUtil.SetInt($"EditorInt_{key}", value);
    public static void Set(string key, float value) => PlayerPrefs.SetFloat($"EditorFloat_{key}", value);
    public static void Set<T>(string key, T value) where T : struct, Enum => PlayerPrefsUtil.Set($"EditorEnum_{key}", value);

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

}
