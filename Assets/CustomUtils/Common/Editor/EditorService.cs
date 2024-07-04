using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[RequiresStaticMethodImplementation("CacheRefresh", typeof(DidReloadScripts))]
public abstract class EditorService : EditorWindow {

    protected virtual string SessionKey => $"{GetType().Name}_FirstOpen";

    private const string ON_LOAD_SESSION_KEY = "EditorServiceOnLoadSessionKey";

    private readonly OverridenMethod _overridenMethod;

    public EditorService() {
        _overridenMethod = new OverridenMethod(GetType(), nameof(OnEnteredEditMode), nameof(OnExitingEditMode), nameof(OnEnteredPlayMode), nameof(OnExitingPlayMode));
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    protected virtual bool CheckSession() => EditorCommon.TryGetSession(SessionKey, out bool isFirstOpen) == false || isFirstOpen == false;
    
    private void OnDestroy() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

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

public class OverridenMethod {

    private readonly HashSet<string> _overrideSet = new();

    public OverridenMethod(Type type, params string[] methods) {
        foreach (var method in methods) {
            if (type.TryGetMethod(method, out var info, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                var declaringType = info.GetBaseDefinition().DeclaringType;
                if (declaringType != info.DeclaringType) {
                    _overrideSet.Add(method);
                }
            }
        }
    }
        
    public bool HasOverriden(Type type) => _overrideSet.Contains(type.Name);
    public bool HasOverriden(string type) => _overrideSet.Contains(type);
}