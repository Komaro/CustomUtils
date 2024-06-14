using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class EditorService : EditorWindow {

    protected virtual string SessionKey => $"{GetType().Name}_FirstOpen";

    private const string ON_LOAD_SESSION_KEY = "EditorServiceOnLoadSessionKey";
    
    [InitializeOnLoadMethod]
    private static void OnEditorLoad() {
        if (EditorCommon.TryGetSession(ON_LOAD_SESSION_KEY, out bool isFirstOpen) == false || isFirstOpen == false) {
            EditorApplication.update += OnEditorFirstOpen;
        }
    }

    private static void OnEditorFirstOpen() {
        EditorApplication.update -= OnEditorFirstOpen;
        foreach (var type in ReflectionManager.GetSubClassTypes<EditorService>()) {
            var objects = Resources.FindObjectsOfTypeAll(type);
            if (objects is { Length: > 0 } && objects.First() is EditorService editorService && editorService.CheckSession()) {
                EditorApplication.update += editorService.OnEditorOpenUpdate;
            }
        }
    }

    protected virtual bool CheckSession() => EditorCommon.TryGetSession(SessionKey, out bool isFirstOpen) == false || isFirstOpen == false;

    protected virtual void OnEditorOpenUpdate() {
        EditorApplication.update -= OnEditorOpenUpdate;
        OnEditorOpenInitialize();
        EditorCommon.SetSession(SessionKey, true);
    }
    
    protected abstract void OnEditorOpenInitialize();
}