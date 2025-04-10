using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

[TestRequired]
public class MonoService : IService {

    private Transform _root;
    private ObjectPool<MonoObject> _pool;

    void IService.Init() {
        var go = new GameObject("MonoService") {
            hideFlags = HideFlags.HideAndDontSave,
        };

        Object.DontDestroyOnLoad(go);
        _root = go.transform;
        
        _pool = new ObjectPool<MonoObject>(OnCreate, OnGet, OnRelease, OnDestroy, defaultCapacity:3, maxSize:10);
    }

    void IService.Start() {
        _root.ThrowIfNull(nameof(_root));
        _pool.ThrowIfNull(nameof(_pool));
    }
    
    void IService.Stop() { }

    public MonoObject Get() => _pool.Get();
    public PooledObject<MonoObject> Get(out MonoObject monoObject) => _pool.Get(out monoObject);
    
    public void Release(MonoObject monoObject) => _pool.Release(monoObject);
    
    private MonoObject OnCreate() => _pool == null ? MonoObject.Create(_root, 0) : MonoObject.Create(_root, (uint)_pool.CountAll);
    private void OnGet(MonoObject monoObject) => monoObject.StartUpdate();
    
    private void OnRelease(MonoObject monoObject) {
        monoObject.ClearAllUpdate();
        monoObject.StopUpdate();
    }

    private void OnDestroy(MonoObject monoObject) => Object.Destroy(monoObject);
}

[TestRequired]
public class MonoObject : MonoBehaviour, IDisposable {
    
    private SafeDelegate<Action> OnUpdate;
    private SafeDelegate<Action> OnFixedUpdate;
    private SafeDelegate<Action> OnLateUpdate;

    public static MonoObject Create(Transform parent, uint index) {
        parent.ThrowIfNull(nameof(parent));
        
        var go = new GameObject($"{nameof(MonoObject)}_{index:00}") {
            hideFlags = HideFlags.HideAndDontSave
        };
        
        go.transform.SetParent(parent);
        go.SetActive(false);
        return go.GetOrAddComponent<MonoObject>();
    }

    private void Update() => OnUpdate.handler?.Invoke();
    private void FixedUpdate() => OnFixedUpdate.handler?.Invoke();
    private void LateUpdate() => OnLateUpdate.handler?.Invoke();

    private void OnDestroy() => Dispose();
    public void Dispose() => GC.SuppressFinalize(this);

    public void StartUpdate() => gameObject.SetActive(true);
    public void StopUpdate() => gameObject.SetActive(false);

    public void AttachUpdate(Action onUpdate) => OnUpdate += onUpdate;
    public void AttachFixedUpdate(Action onUpdate) => OnFixedUpdate += onUpdate;
    public void AttachLateUpdate(Action onUpdate) => OnLateUpdate += onUpdate;

    public void ClearAllUpdate() {
        ClearUpdate();
        ClearFixedUpdate();
        ClearLateUpdate();
    }
    
    public void ClearUpdate() => OnUpdate.Clear();
    public void ClearFixedUpdate() => OnFixedUpdate.Clear();
    public void ClearLateUpdate() => OnLateUpdate.Clear();
}
