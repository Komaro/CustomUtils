using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class EditorCommon {
    
    private static string _unityProjectPath;
    public static string UNITY_PROJECT_PATH => string.IsNullOrEmpty(_unityProjectPath) ? _unityProjectPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityProjectPath;
    
    public static void ShowCheckDialogue(string title, string message, string okText = "확인", string cancelText = "취소", Action ok = null, Action cancel = null) {
        if (EditorUtility.DisplayDialog(title, message, okText, cancelText)) {
            ok?.Invoke();
        } else {
            cancel?.Invoke();
        }
    }

    public static void DrawSeparator() {
        GUILayout.Space(10);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), Color.gray);
        GUILayout.Space(10);
    }

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

    public static bool TryLoadJson<T>(string path, out T json) {
        try {
            json = LoadJson<T>(path);
            return json != null;
        } catch (Exception e) {
            Debug.LogError(e);

            json = default;
            return false;
        }
    }
    
    public static T LoadJson<T>(string path) {
        try {
            if (File.Exists(path) == false) {
                Debug.LogError($"Invalid Path || {path}");
                throw new FileNotFoundException();
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrEmpty(text) == false) {
                var json = JsonConvert.DeserializeObject<T>(text);
                return json;
            }
        }  catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
        
        return default;
    }
    
    public static void SaveJson(string path, JObject json) {
        try {
            if (json == null) {
                throw new NullReferenceException($"{nameof(json)} is Null");
            }

            SaveJson(path, json.ToString());
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }

    public static void SaveJson(string path, object ob) {
        try {
            if (ob == null) {
                throw new NullReferenceException($"{nameof(ob)} is Null");
            }

            var json = JsonConvert.SerializeObject(ob);
            if (string.IsNullOrEmpty(json)) {
                throw new JsonException("Serialization failed. An empty result was returned.");
            }
            
            SaveJson(path, json);
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }

    public static void SaveJson(string path, string json) {
        try {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(json)) {
                Debug.LogError($"{nameof(path)} or {nameof(json)} is Null or Empty");
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
            
            File.WriteAllText(path, json);
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }
}
