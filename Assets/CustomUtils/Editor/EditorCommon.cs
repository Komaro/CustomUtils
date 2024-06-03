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

    public static void DrawLabelTextSet(string label, string text, float labelWidth = 100) {
        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label(label, Constants.Editor.FIELD_TITLE_STYLE, GUILayout.Width(labelWidth));
            GUILayout.TextField(text);
        }
    }
    
    public static string DrawInputFieldSet(string label, string text, float labelWidth = 100) {
        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label(label, Constants.Editor.FIELD_TITLE_STYLE, GUILayout.Width(labelWidth));
            text = GUILayout.TextField(text);
        }
        
        return text;
    }

    public static string DrawFolderSelector(string text, string targetDirectory, Action<string> onSelect = null, float width = 120) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(text, GUILayout.Width(width))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("대상 폴더", targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    targetDirectory = selectDirectory;
                }
                
                onSelect?.Invoke(targetDirectory);
            }
            
            GUILayout.TextField(targetDirectory);
        }
        
        return targetDirectory;
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

}
