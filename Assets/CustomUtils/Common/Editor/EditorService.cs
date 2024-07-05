using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[RequiresStaticMethodImplementation("CacheRefresh", typeof(DidReloadScripts))]
public abstract class EditorService : EditorWindow {

    protected virtual string SessionKey => $"{GetType().Name}_FirstOpen";

    private const string ON_LOAD_SESSION_KEY = "EditorServiceOnLoadSessionKey";

    private OverridenMethod _overridenMethod;

    public EditorService() {
        _overridenMethod = new OverridenMethod(GetType(), nameof(OnEnteredEditMode), nameof(OnExitingEditMode), nameof(OnEnteredPlayMode), nameof(OnExitingPlayMode));
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    protected virtual bool CheckSession() => EditorCommon.TryGetSession(SessionKey, out bool isFirstOpen) == false || isFirstOpen == false;
    
    private void OnDestroy() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

    // TODO. InitializeOnLoad 로 전환 테스트
    [InitializeOnLoadMethod]
    private static void OnEditorLoad() {
        if (EditorCommon.TryGetSession(ON_LOAD_SESSION_KEY, out bool isFirstOpen) == false || isFirstOpen == false) {
            EditorApplication.update += OnEditorFirstOpen;
        }
    }

    private static void OnEditorFirstOpen() {
        EditorApplication.update -= OnEditorFirstOpen;
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
                if (_overridenMethod.HasOverriden(nameof(OnEnteredEditMode))) {
                    OnEnteredEditMode();
                }
                break;
            case PlayModeStateChange.ExitingEditMode:
                if (_overridenMethod.HasOverriden(nameof(OnExitingEditMode))) {
                    OnExitingEditMode();
                }
                break;
            case PlayModeStateChange.EnteredPlayMode:
                if (_overridenMethod.HasOverriden(nameof(OnEnteredEditMode))) {
                    OnEnteredPlayMode();
                }
                break;
            case PlayModeStateChange.ExitingPlayMode:
                if (_overridenMethod.HasOverriden(nameof(OnExitingPlayMode))) {
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