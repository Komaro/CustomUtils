using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

public class ObjectPoolService : IService {

    private GameObject _poolRoot;
    private ConcurrentDictionary<string, ObjectPoolBag> _poolDic = new();

    private bool _isServing = false;

    void IService.Init() {
        _poolRoot = new GameObject("ObjectPoolService") {
            hideFlags = HideFlags.DontSave | HideFlags.NotEditable
        };
        
        Object.DontDestroyOnLoad(_poolRoot);
    }

    void IService.Start() {
        _isServing = true;
    }

    void IService.Stop() {
        _isServing = false;
    }

    void IService.Remove() {
        _poolDic.SafeClear(pool => pool.Dispose());

        if (_poolRoot.activeInHierarchy) {
            Object.Destroy(_poolRoot);
        }
    }

    public void Preload(string name, int count) {
        var pool = _poolDic.GetOrAdd(name, CreatePool);
        if (pool != null) {
            for (var i = 0; i < count; i++) {
                pool.Release(pool.Get());
            }
        }
    }

    public GameObject Get(string name) => _poolDic.GetOrAdd(name, CreatePool)?.Get();

    public void Release(GameObject go) {
        if (go == null) {
            Logger.TraceError($"{nameof(go)}({go.name}) is null");
            return;
        }
        
        if (go.TryGetComponent<ObjectPool>(out var objectPool) == false) {
            Logger.TraceError($"{nameof(go)}({go.name}) is not a type managed by {nameof(ObjectPoolService)}");
            return;
        }

        if (_poolDic.TryGetValue(objectPool.GetName(), out var pool) == false) {
            Logger.TraceError($"Invalid name || {objectPool.GetName()}");
            return;
        }
        
        pool.Release(go);
    }

    private ObjectPoolBag CreatePool(string name) => new(_poolRoot, name, 50);

    public int GetCountAll(string name) => _poolDic.TryGetValue(name, out var pool) ? pool.CountAll : 0;
    public int GetCountActive(string name) => _poolDic.TryGetValue(name, out var pool) ? pool.CountActive : 0;
    public int GetCountInactive(string name) => _poolDic.TryGetValue(name, out var pool) ? pool.CountInactive : 0;
    public int GetMaxSize(string name) => _poolDic.TryGetValue(name, out var pool) ? pool.MaxSize : 0;
} 

public record ObjectPoolBag : IDisposable {

    private readonly GameObject _prefab;

    private readonly GameObject _poolRoot;
    private readonly ObjectPool<GameObject> _pool;

    public int CountAll => _pool.CountAll;
    public int CountActive => _pool.CountActive;
    public int CountInactive => _pool.CountInactive;
    public int MaxSize { get; }
    
    public ObjectPoolBag(GameObject root, string name, int max) {
        _prefab = Service.GetService<ResourceService>().Instantiate(name);
        if (_prefab != null) {
            _prefab.name = name;
            var pool = _prefab.GetOrAddComponent<ObjectPool>();
            if (pool != null) {
                pool.SetName(name);
            }
            
            _prefab.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_prefab);
        }
        
        _poolRoot = new GameObject($"{name}");
        _poolRoot.transform.SetParent(root.transform);

        MaxSize = max;
        _pool = new ObjectPool<GameObject>(OnCreateObject, OnGetGameObject, OnReleaseGameObject, OnDestroyGameObject, maxSize:MaxSize);
    }

    public void Dispose() {
        _pool?.Dispose();
        Object.Destroy(_prefab);
        Object.Destroy(_poolRoot);
    }

    public GameObject Get() => _pool?.Get();
    public void Release(GameObject go) => _pool?.Release(go);

    public bool IsValid() => _prefab != null && _prefab.TryGetComponent<ObjectPool>(out _);
    
    private GameObject OnCreateObject() {
        if (_prefab != null) {
            var go = Object.Instantiate(_prefab);
            go.name = $"{go.name}_{_pool.CountActive:D3}"; 
            return go;
        }
        
        return null;
    }

    private void OnGetGameObject(GameObject go) => go.SetActive(true);
    
    private void OnReleaseGameObject(GameObject go) {
        go.SetActive(false);
        go.transform.SetParent(_poolRoot.transform);
    }

    private void OnDestroyGameObject(GameObject go) => Object.Destroy(go);
}

public class ObjectPool : MonoBehaviour {

    [SerializeReference]
    private string _name;

    public void SetName(string name) => _name = name;
    public string GetName() => _name;
}