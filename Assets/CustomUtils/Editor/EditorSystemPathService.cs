using System.Collections.Immutable;
using UnityEditor;
using UnityEngine;

public class EditorSystemPathService : EditorService<EditorSystemPathService> {

    [MenuItem("Service/System Path Service")]
    private static void OpenWindow() => Window.Open();

    private ImmutableDictionary<string, string> _pathDic;

    protected override void Refresh() {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        builder.Add(nameof(Application.dataPath), Application.dataPath);
        builder.Add(nameof(Application.persistentDataPath), Application.persistentDataPath);
        builder.Add(nameof(Application.consoleLogPath), Application.consoleLogPath);
        builder.Add(nameof(Application.streamingAssetsPath), Application.streamingAssetsPath);
        builder.Add(nameof(Application.temporaryCachePath), Application.temporaryCachePath);

        _pathDic = builder.ToImmutable();
    }

    private void OnGUI() {
        using (new EditorGUILayout.VerticalScope()) {
            if (_pathDic.Count > 0) {
                foreach (var (pathName, path) in _pathDic) {
                    EditorCommon.DrawLabelLinkButton(pathName, path, EditorUtility.RevealInFinder, 150f);
                }
            }
        }
    }
}
