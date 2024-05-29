using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IResourceProvider {

    bool Valid();
    void Init();
    void Load();
    void Unload(Dictionary<string, Object> cacheResource);
    Object Get(string name);
    string GetPath(string name);
    bool IsLoaded();
}

[Service(DEFAULT_SERVICE_TYPE.RESOURCE_LOAD)]
public class ResourceService : IService {

    private IResourceProvider _provider;
    private IResourceProvider _subProvider;
    private Dictionary<string, Object> _cacheResourceDic = new();

    private bool _isServing;
    private bool _isActiveSubProvider;
    
    public bool IsServing() => _isServing;

    public void Init() {
        try {
            var providerTypeList = ReflectionManager.GetInterfaceTypes<IResourceProvider>().ToList();
            _isActiveSubProvider = ReflectionManager.GetAttribute<ResourceSubProviderAttribute>().Any();
            if (_isActiveSubProvider) {
                Logger.TraceLog($"SubProvider is activated. Find {nameof(ResourceSubProviderAttribute)}...", Color.yellow);
                _subProvider = Init(providerTypeList.Where(x => x.ContainsCustomAttribute<ResourceSubProviderAttribute>()).OrderBy(x => x.GetCustomAttribute<ResourceSubProviderAttribute>().order));
                if (_subProvider == null) {
                    Logger.TraceError($"{nameof(_subProvider)} is Null. Check {nameof(ResourceSubProviderAttribute)} Implementation");
                }
            }
            
            _provider = Init(providerTypeList.Where(x => x.ContainsCustomAttribute<ResourceProviderAttribute>()).OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>().order));
        } catch (Exception ex) {
            Logger.TraceError(ex);
            if (_provider == null || _provider.IsLoaded() == false) {
                Logger.TraceLog($"Provider is Invalid. Temporarily create {nameof(NullResourceProvider)}");
                _provider = new NullResourceProvider();
                _provider.Init();
            }
        }
    }

    private IResourceProvider Init(IEnumerable<Type> enumerable) {
        foreach (var type in enumerable) {
            if (Activator.CreateInstance(type) is IResourceProvider provider && provider.Valid()) {
                provider.Init();
                Logger.TraceLog($"Activate Provider || {type.Name} || {type.Name}", Color.cyan);
                return provider;
            }
        }
        
        Logger.TraceError($"Failed to find a valid provider. Check {nameof(IResourceProvider)}.{nameof(IResourceProvider.Valid)} Method Implementation");
        Logger.TraceLog($"Temporarily create {nameof(NullResourceProvider)}", Color.red);
        var nullProvider = new NullResourceProvider();
        nullProvider.Init();
        return nullProvider;
    }

    public void Start() {
        if (_isActiveSubProvider && _subProvider.IsLoaded() == false) {
            _subProvider.Load();
        }

        if (_provider.IsLoaded() == false) {
            _provider.Load();
        }

        _isServing = true;
    }

    public void Stop() { }

    public void Refresh() {
        
    }

    public void Remove() {
        if (_isActiveSubProvider) {
            _subProvider.Unload(_cacheResourceDic);
        }
        
        _provider.Unload(_cacheResourceDic);

        _isServing = false;
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
        if (_cacheResourceDic.TryGetValue(name, out var ob)) {
            return ob;
        }

        ob = _provider?.Get(name) ?? _subProvider?.Get(name);
        if (ob == null) {
            Logger.TraceError($"{nameof(ob)} is Null. Missing Resource || {name}");
            return default;
        }
        
        _cacheResourceDic.AutoAdd(name, ob);
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

[AttributeUsage(AttributeTargets.Class)]
public class ResourceProviderAttribute : Attribute {

    public readonly int order;

    public ResourceProviderAttribute(int order = 0) => this.order = order;
}

[AttributeUsage(AttributeTargets.Class)]
public class ResourceSubProviderAttribute : Attribute {

    public readonly int order;

    public ResourceSubProviderAttribute(int order = 0) => this.order = order;
}