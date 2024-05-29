using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class EditorSystemService : EditorWindow {

    private void OnGUI() {
        
        // Logger.TraceLog(Application.dataPath);
        // Logger.TraceLog(Application.persistentDataPath);
        // Logger.TraceLog(Application.consoleLogPath);
        // Logger.TraceLog(Application.streamingAssetsPath);
        // Logger.TraceLog(Application.temporaryCachePath);
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField($"{nameof(Application.dataPath)} : ", Constants.Editor.WHITE_BOLD_STYLE);
            DrawLinkButton(Application.dataPath);
        }

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField($"{nameof(Application.persistentDataPath)} : ", Constants.Editor.WHITE_BOLD_STYLE);
            DrawLinkButton(Application.persistentDataPath);
        }
        
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField($"{nameof(Application.consoleLogPath)} : ", Constants.Editor.WHITE_BOLD_STYLE);
            DrawLinkButton(Application.consoleLogPath);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField($"{nameof(Application.streamingAssetsPath)} : ", Constants.Editor.WHITE_BOLD_STYLE);
            DrawLinkButton(Application.streamingAssetsPath);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField($"{nameof(Application.temporaryCachePath)} : ", Constants.Editor.WHITE_BOLD_STYLE);
            DrawLinkButton(Application.temporaryCachePath);
        }
    }

    [MenuItem("Service/SystemService")]
    public static void OpenWindow() {
        var window = GetWindow<EditorSystemService>("SystemService");
        window.Show();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLinkButton(string path) {
        if (EditorGUILayout.LinkButton(path)) {
            EditorUtility.RevealInFinder(path);
        }
        
        EditorGUILayout.Space();
    }
}
