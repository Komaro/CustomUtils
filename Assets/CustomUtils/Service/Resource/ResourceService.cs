using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IResourceProvider {
    void Init();
    void Load();
    Object Get(string name);
    string GetPath(string name);
}

[Service(DEFAULT_SERVICE_TYPE.RESOURCE_LOAD)]
public class ResourceService : IService {

    private IResourceProvider _provider;
    private Dictionary<string, Object> _cacheResource = new();

    public void Init() {
        // TODO. Create Auto Provider Selector
        _provider = new ResourcesProvider();
        _provider.Init();
    }

    public void Start() {
        _provider.Load();
    }

    public void Stop() {
        
    }

    public void Refresh() {
        
    }

    public void Remove() {
        // Unload All Asset
    }

    public bool TryGet(string name, out GameObject go) {
        go = Get(name);
        return go != null;
    }
    
    public GameObject Get(string name) => GetObject(name) as GameObject;

    public bool TryGet<T>(string name, out T ob) where T : Object {
        ob = Get<T>(name);
        return ob != null;
    }
    
    public T Get<T>(string name) where T : Object {
        var ob = GetObject(name);
        if (ob is T outObject) {
            return outObject;
        }
        
        return null;
    }
    
    public Object GetObject(string name) {
        if (_cacheResource.TryGetValue(name, out var ob)) {
            return ob;
        }

        ob = _provider.Get(name);
        if (ob == null) {
            Logger.TraceError($"{nameof(ob)} is Null. Missing Resource || {name}");
            return default;
        }
        
        _cacheResource.AutoAdd(name, ob);
        return ob;
    }

    public bool TryGetPath(string name, out string path) {
        path = GetPath(name);
        return string.IsNullOrEmpty(path) == false;
    }
    
    public string GetPath(string name) => _provider.GetPath(name);
    
    public T Instantiate<T>(string name, GameObject parent, bool isAddComponent = false) where T : Component => Instantiate<T>(name, parent.transform, isAddComponent);
    
    public T Instantiate<T>(string name, Transform parent, bool isAddComponent = false) where T : Component {
        var instant = Instantiate(name, parent);
        if (instant == null) {
            return null;
        }

        if (instant.TryGetComponent<T>(out var component)) {
            return component;
        }

        if (isAddComponent) {
            return instant.AddComponent<T>();
        }

        Logger.TraceError($"Component Missing || {nameof(T)} || {name}");
        return null;
    }

    public GameObject Instantiate(string name, GameObject parent) => Instantiate(name, parent.transform);
    
    public GameObject Instantiate(string name, Transform parent) {
        var go = Get(name);
        if (go == null) {
            return null;
        }

        var instant = Object.Instantiate(go, parent);
        if (instant == null) {
            Logger.TraceError($"{nameof(instant)} is Null");
            return null;
        }

        instant.name = name;
        return instant;
    }
}
