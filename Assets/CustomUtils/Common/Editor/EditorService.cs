using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[RequiresStaticMethodImplementation("CacheRefresh", typeof(DidReloadScripts))]
public abstract class EditorService : EditorWindow {

    protected virtual string SessionKey => $"{GetType().Name}_FirstOpen";

    private const string EDITOR_FIRST_OPEN_SESSION_KEY = "EditorServiceFirstOpenKey";

    protected OverridenMethod overridenMethod;

    protected EditorService() {
        overridenMethod = new OverridenMethod(GetType(), nameof(OnEnteredEditMode), nameof(OnExitingEditMode), nameof(OnEnteredPlayMode), nameof(OnExitingPlayMode));
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    protected virtual bool CheckSession() => EditorCommon.TryGetSession(SessionKey, out bool isFirstOpen) == false || isFirstOpen == false;
    
    private void OnDestroy() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorCommon.TryGetSession(EDITOR_FIRST_OPEN_SESSION_KEY, out bool isFirstOpen) == false || isFirstOpen == false) {
            EditorApplication.update += OnEditorFirstOpen;
        }
    }
    
    private static void OnEditorFirstOpen() {
        EditorApplication.update -= OnEditorFirstOpen;
        EditorCommon.SetSession(EDITOR_FIRST_OPEN_SESSION_KEY, true);
        foreach (var type in ReflectionProvider.GetSubClassTypes<EditorService>()) {
            var objects = Resources.FindObjectsOfTypeAll(type);
            if (objects is { Length: > 0 } && objects.First() is EditorService editorService && editorService.CheckSession()) {
                EditorApplication.update += editorService.OnEditorOpenUpdate;
            }
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state) {
        switch (state) {
            case PlayModeStateChange.EnteredEditMode:
                if (overridenMethod.HasOverriden(nameof(OnEnteredEditMode))) {
                    OnEnteredEditMode();
                }
                break;
            case PlayModeStateChange.ExitingEditMode:
                if (overridenMethod.HasOverriden(nameof(OnExitingEditMode))) {
                    OnExitingEditMode();
                }
                break;
            case PlayModeStateChange.EnteredPlayMode:
                if (overridenMethod.HasOverriden(nameof(OnEnteredEditMode))) {
                    OnEnteredPlayMode();
                }
                break;
            case PlayModeStateChange.ExitingPlayMode:
                if (overridenMethod.HasOverriden(nameof(OnExitingPlayMode))) {
                    OnExitingPlayMode();
                }
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
}