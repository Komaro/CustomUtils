using UnityEditor;

public static class EditorCopyPath {
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
    public static bool CopyPathValidation() {
        return Selection.gameObjects.Length == 1;
    }
}