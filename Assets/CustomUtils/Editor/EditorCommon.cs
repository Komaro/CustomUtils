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

    public static bool GetBool(string key) => PlayerPrefsUtil.TryGet($"EditorBool_{key}", out bool value) && value;
    public static void GetBool(string key, bool value) => PlayerPrefsUtil.Set($"EditorBool_{key}", value);

    public static int GetInt(string key) => PlayerPrefsUtil.GetInt($"EditorInt_{key}");
    public static void SetInt(string key, int value) => PlayerPrefsUtil.SetInt($"EditorInt_{key}", value);

    public static string GetString(string key) => PlayerPrefsUtil.GetString($"EditorString_{key}");
    public static void SetString(string key, string value) => PlayerPrefsUtil.SetString($"EditorString_{key}", value);

    public static float GetFloat(string key) => PlayerPrefs.GetFloat($"EditorFloat_{key}");
    public static void SetFloat(string key, float value) => PlayerPrefs.SetFloat($"EditorFloat_{key}", value);
    
    public static T GetEnum<T>(string key) where T : struct, Enum => PlayerPrefsUtil.TryGet<T>(key, out var value) ? value : default;
    public static void SetEnum<T>(string key, T value) where T : struct, Enum => PlayerPrefsUtil.Set(key, value);
    
    #endregion
}
