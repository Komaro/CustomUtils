using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

public class ObjectPoolService : IService {

    private GameObject _poolRoot;
    private ConcurrentDictionary<string, ObjectPoolBag> _poolDic = new();
    private ConcurrentDictionary<GameObject, ObjectPoolBag> _mappingDic = new();

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
        if (go != null && go.TryGetComponent<ObjectPool>(out var objectPool) && _poolDic.TryGetValue(objectPool.GetName(), out var pool)) {
            pool.Release(go);
        }
    }

    private ObjectPoolBag CreatePool(string name) => new(_poolRoot, name, 50);
} 

public record ObjectPoolBag : IDisposable {

    private readonly GameObject _prefab;

    private readonly GameObject _poolRoot;
    private readonly ObjectPool<GameObject> _pool;

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
        
        _pool = new ObjectPool<GameObject>(CreateObject, GetGameObject, ReleaseGameObject, DestroyGameObject, maxSize:max);
    }

    public void Dispose() {
        _pool?.Dispose();

        if (Application.isPlaying) {
            if (_prefab.activeInHierarchy) {
                Object.Destroy(_prefab);
            }

            if (_poolRoot.activeInHierarchy) {
                Object.Destroy(_poolRoot);
            }
        }
    }

    public GameObject Get() => _pool?.Get();
    public void Release(GameObject go) => _pool?.Release(go);

    private GameObject CreateObject() {
        if (_prefab != null) {
            var go = Object.Instantiate(_prefab);
            go.name = $"{go.name}_{_pool.CountActive:D3}"; 
            return go;
        }
        
        return null;
    }
    
    private void GetGameObject(GameObject go) => go.SetActive(true);
    
    private void ReleaseGameObject(GameObject go) {
        go.SetActive(false);
        go.transform.SetParent(_poolRoot.transform);
    }

    private void DestroyGameObject(GameObject go) {
        Object.Destroy(go);
    }

    public bool IsValid() => _prefab != null && _prefab.TryGetComponent<ObjectPool>(out _);
}

public class ObjectPool : MonoBehaviour {

    [SerializeReference]
    private string _name;

    public void SetName(string name) => _name = name;
    public string GetName() => _name;
}