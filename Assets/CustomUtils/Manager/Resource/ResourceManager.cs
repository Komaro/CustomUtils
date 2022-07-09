using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager> {

    private IResourceProvider _provider;
    private Dictionary<string, Object> _cacheResource = new Dictionary<string, Object>();
    
    public void Init(IResourceProvider provider) {
        _provider = provider;
        _provider.Init();
    }

    public void LoadResource() => _provider?.Load();

    public Object GetObject(string name) {
        if (_cacheResource.TryGetValue(name, out var ob)) {
            return ob as GameObject;
        }

        ob = _provider.Get(name);
        if (ob == null) {
            Logger.TraceError($"{nameof(ob)} is Null. Missing Resource || {name}");
            return default;
        }

        return ob;
    }
    
    public GameObject Get(string name) => GetObject(name) as GameObject;
    
    public T Get<T>(string name) where T : Object {
        var ob = GetObject(name);
        if (ob is T outObject) {
            return outObject;
        }
        
        return null;
    }
    
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
        } else {
            Logger.TraceError($"Component Missing || {nameof(T)} || {name}");               
        }

        return null;
    }

    public GameObject Instantiate(string name, GameObject parent) => Instantiate(name, parent.transform);
    public GameObject Instantiate(string name, Transform parent) {
        var ob = Get(name);
        if (ob == default) {
            return null;
        }

        var instant = Object.Instantiate(ob, parent);
        if (instant == null) {
            Logger.TraceError($"{nameof(instant)} is Null");
            return null;
        }

        instant.name = name;
        return instant;
    }
}
