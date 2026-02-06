using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

// TODO. 기존의 EditorWindow에서 전환하는 경우 Constants 접근에서 문제가 발생. 기존의 코드는 DidScriptReload를 통해 static 메소드로 호출되었기 때문에 문제가 없었던 것으로 보이나 OnEnable로 처리를 옮기는 경우 필드에서 Application.dataPath와 같은 api에 접근하는 것은 불가능하므로 다른 형태로 초기화 할 필요가 있음. 
[RequiresStaticMethodImplementation("OpenWindow", typeof(MenuItem))]
public abstract class EditorService<T> : EditorWindow where T : EditorService<T> {

    private static T _window;
    protected static T Window => _window == null ? _window = GetWindow<T>(typeof(T).Name) : _window;

    protected readonly CancellationTokenSource _tokenSource = new();

    // TODO. PlayMode -> EditMode 전환시와 같은 경우 호출되지 않음. EditMode -> PlayMode로 전환 시 도메인을 새롭게 로드하기 때문에 전체 Service가 무효화 됨
    // TODO. PlayMode 시에도 동작해야 하는 경우와 아닌 경우를 나눠 새롭게 구조를 재편할 필요가 있음 
    protected virtual void OnEnable() {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Refresh();
    }

    protected virtual void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        _tokenSource.Cancel();
    }

    private void OnDestroy() => _tokenSource.Dispose();

    protected virtual void Open() {
        if (_window != null) {
            _window.Show();
            _window.Focus();
        }
    }

    protected abstract void Refresh();

    protected virtual AsyncCustomOperation AsyncRefresh() {
        var operation = new AsyncCustomOperation();
        _ = AsyncRefresh(operation, _tokenSource.Token);
        return operation;
    }
    
    protected virtual Task AsyncRefresh(AsyncCustomOperation operation, CancellationToken token) => throw new NotImplementedException();

    public bool HasOpenInstances() => HasOpenInstances<T>();
    
    protected virtual void OnPlayModeStateChanged(PlayModeStateChange state) {
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

    protected virtual void OnEnteredEditMode() => Refresh();
    protected virtual void OnExitingEditMode() { }
    protected virtual void OnEnteredPlayMode() { }
    protected virtual void OnExitingPlayMode() { }

    protected EditorCoroutine StartCoroutine(IEnumerator enumerator, object owner = null) => EditorCoroutineUtility.StartCoroutine(enumerator, owner ?? this);
}

[Obsolete("EditorService<T> 전환 필요하나 OnEnable 처리 플로우에 문제가 있음")]
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