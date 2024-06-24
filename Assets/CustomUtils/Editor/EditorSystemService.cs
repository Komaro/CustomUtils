using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class EditorSystemService : EditorWindow {

    [MenuItem("Service/System Service")]
    private static void OpenWindow() {
        var window = GetWindow<EditorSystemService>("SystemService");
        window.Show();
    }

    private void OnGUI() {
        using (new GUILayout.VerticalScope(GUILayout.ExpandHeight(false))) {
            EditorCommon.DrawLabelLinkButton($"{nameof(Application.dataPath)} : ", Application.dataPath, EditorUtility.RevealInFinder, 150f);
            EditorCommon.DrawLabelLinkButton($"{nameof(Application.persistentDataPath)} : ", Application.persistentDataPath, EditorUtility.RevealInFinder, 150f);
            EditorCommon.DrawLabelLinkButton($"{nameof(Application.consoleLogPath)} : ", Application.consoleLogPath, EditorUtility.RevealInFinder, 150f);
            EditorCommon.DrawLabelLinkButton($"{nameof(Application.streamingAssetsPath)} : ", Application.streamingAssetsPath, EditorUtility.RevealInFinder, 150f);
            EditorCommon.DrawLabelLinkButton($"{nameof(Application.temporaryCachePath)} : ", Application.temporaryCachePath, EditorUtility.RevealInFinder, 150f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLinkButton(string path) {
        if (EditorGUILayout.LinkButton(path)) {
            EditorUtility.RevealInFinder(path);
        }
        
        EditorGUILayout.Space();
    }
}
