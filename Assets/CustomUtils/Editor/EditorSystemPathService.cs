using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorSystemPathService : EditorService<EditorSystemPathService> {

    [MenuItem("Service/System Path Service")]
    private static void OpenWindow() => Window.Open();
    
    private Dictionary<string, string> _pathDic = new();

    protected override void Refresh() {
        _pathDic.Clear();
        _pathDic.Add(nameof(Application.dataPath), Application.dataPath);
        _pathDic.Add(nameof(Application.persistentDataPath), Application.persistentDataPath);
        _pathDic.Add(nameof(Application.consoleLogPath), Application.consoleLogPath);
        _pathDic.Add(nameof(Application.streamingAssetsPath), Application.streamingAssetsPath);
        _pathDic.Add(nameof(Application.temporaryCachePath), Application.temporaryCachePath);
    }

    private void OnGUI() {
        using (new EditorGUILayout.VerticalScope()) {
            if (_pathDic.Count > 0) {
                foreach (var pair in _pathDic) {
                    EditorCommon.DrawLabelLinkButton(pair.Key, pair.Value, EditorUtility.RevealInFinder, 150f);
                }
            }
        }
    }
}
