using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

[TestRequired]
public class MonoUpdateService : IService {

    private Transform _root;
    private ObjectPool<UpdateObject> _pool;

    void IService.Init() {
        var go = new GameObject("MonoUpdateService") {
            hideFlags = HideFlags.HideAndDontSave,
        };

        Object.DontDestroyOnLoad(go);
        _root = go.transform;
        
        _pool = new ObjectPool<UpdateObject>(OnCreate, OnGet, OnRelease, OnDestroy, defaultCapacity:3, maxSize:10);
    }

    void IService.Start() {
        _root.ThrowIfNull(nameof(_root));
        _pool.ThrowIfNull(nameof(_pool));
    }
    
    void IService.Stop() { }

    public UpdateObject Get() => _pool.Get();
    public PooledObject<UpdateObject> Get(out UpdateObject updateObject) => _pool.Get(out updateObject);
    
    public void Release(UpdateObject updateObject) => _pool.Release(updateObject);
    
    private UpdateObject OnCreate() => _pool == null ? UpdateObject.Create(_root, 0) : UpdateObject.Create(_root, (uint)_pool.CountAll);
    private void OnGet(UpdateObject updateObject) => updateObject.gameObject.SetActive(true);
    private void OnRelease(UpdateObject updateObject) => updateObject.StopUpdate();
    private void OnDestroy(UpdateObject updateObject) => Object.Destroy(updateObject);
}

[TestRequired]
public class UpdateObject : MonoBehaviour, IDisposable {
    
    private SafeDelegate<Action> OnUpdate;
    
    public static UpdateObject Create(Transform parent, uint index) {
        parent.ThrowIfNull(nameof(parent));
        
        var go = new GameObject($"{nameof(UpdateObject)}_{index:00}") {
            hideFlags = HideFlags.HideAndDontSave
        };
        
        go.transform.SetParent(parent);
        go.SetActive(false);
        return go.GetOrAddComponent<UpdateObject>();
    }

    private void Update() => OnUpdate.handler?.Invoke();
    private void OnDestroy() => Dispose();
    
    public void Dispose() => GC.SuppressFinalize(this);

    public void AttachUpdate(Action onUpdate) => OnUpdate += onUpdate;

    public void StartUpdate() => gameObject.SetActive(true);

    public void StopUpdate() {
        OnUpdate.Clear();
        gameObject.SetActive(false);
    }
}
