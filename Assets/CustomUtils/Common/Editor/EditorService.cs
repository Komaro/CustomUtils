using System.Collections;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[RequiresStaticMethodImplementation("OpenWindow", typeof(MenuItem))]
[RequiresStaticMethodImplementation("CacheRefresh", typeof(DidReloadScripts))]
public abstract class EditorService : EditorWindow {

    protected virtual string SessionKey => $"{GetType().Name}_FirstOpen";

    private const string EDITOR_SERVICE_FIRST_OPEN_SESSION_KEY = "EditorServiceFirstOpenKey";

    protected EditorService() {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    protected virtual bool CheckSession() => EditorCommon.TryGetSession(SessionKey, out bool isFirstOpen) == false || isFirstOpen == false;
    
    private void OnDestroy() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorCommon.TryGetSession(EDITOR_SERVICE_FIRST_OPEN_SESSION_KEY, out bool isFirstOpen) == false || isFirstOpen == false) {
            EditorApplication.update += OnEditorFirstOpen;
        }
    }
    
    private static void OnEditorFirstOpen() {
        EditorApplication.update -= OnEditorFirstOpen;
        EditorCommon.SetSession(EDITOR_SERVICE_FIRST_OPEN_SESSION_KEY, true);
        foreach (var type in ReflectionProvider.GetSubTypesOfType<EditorService>().Where(type => type.IsAbstract == false)) {
            var objects = Resources.FindObjectsOfTypeAll(type);
            if (objects is { Length: > 0 } && objects.First() is EditorService editorService && editorService.CheckSession()) {
                EditorApplication.update += editorService.OnEditorOpenUpdate;
            }
        }
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state) {
        switch (state) {
            case PlayModeStateChange.EnteredEditMode:
                OnEnteredEditMode();
                break;
            case PlayModeStateChange.ExitingEditMode:
                OnExitingEditMode();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                OnEnteredPlayMode();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                OnExitingPlayMode();
                break;
        }
    }

    protected virtual void OnEditorOpenUpdate() {
        EditorApplication.update -= OnEditorOpenUpdate;
        OnEditorOpenInitialize();
        EditorCommon.SetSession(SessionKey, true);
    }
    
    protected abstract void OnEditorOpenInitialize();
    protected virtual void OnEnteredEditMode() { }
    protected virtual void OnExitingEditMode() { }
    protected virtual void OnEnteredPlayMode() { }
    protected virtual void OnExitingPlayMode() { }

    protected EditorCoroutine StartCoroutine(IEnumerator enumerator, object owner = null) => EditorCoroutineUtility.StartCoroutine(enumerator, owner ?? this);
}