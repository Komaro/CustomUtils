using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

[RequiresAttributeImplementation(typeof(ResourceProviderAttribute))]
public interface IResourceProvider : IImplementNullable {

    bool Valid();
    void Init();
    // void Load();
    void Load(ResourceProviderOrder order);
    // void Unload(IDictionary<string, Object> cacheResource);
    void Unload();
    void Unload(ResourceProviderOrder order);
    Object Get(string name);
    string GetPath(string name);
    // bool IsLoaded();
}

[RequiresAttributeImplementation(typeof(ResourceProviderAttribute))]
public interface IResourceCacheProvider : IImplementNullable {

    bool Valid();
    void Init();
    void Unload(ResourceProviderOrder order);
    object Get(string name);
    void Add(string name, object ob);
}

[RequiresAttributeImplementation(typeof(ResourceProviderAttribute))]
public abstract class ResourceProviderModule {

    private IResourceProvider _resourceProvider;
    private IResourceCacheProvider _cacheProvider;

    public ResourceProviderModule() {
        // TODO. Create ResourceProvider & CacheProvider
    }

    public virtual void Load(ResourceProviderOrder order) => _resourceProvider.Load(order);

    public virtual void Unload(ResourceProviderOrder order) {
        _resourceProvider.Unload(order);
        _cacheProvider.Unload(order);
    }

    public virtual object Get(string name) {
        var ob = _cacheProvider.Get(name);
        return ob ?? _resourceProvider.Get(name);
    }
}

[Service(DEFAULT_SERVICE_TYPE.RESOURCE_LOAD)]
public class ResourceService : IService {

    private ResourceProviderModule _module;
    private ResourceProviderModule _subModule;

    private IResourceProvider _provider;
    private IResourceProvider _subProvider;
    private ConcurrentDictionary<string, Object> _cacheResourceDic = new();

    private IResourceCacheProvider _cacheProvider;
    private IResourceCacheProvider _subCacheProvider;

    private bool _isServing;
    private bool _isActiveSubProvider;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        try {
            var providerTypeList = ReflectionProvider.GetInterfaceTypes<IResourceProvider>().ToList();
            _isActiveSubProvider = providerTypeList.Any(type => type.IsDefined<ResourceSubProviderAttribute>());
            if (_isActiveSubProvider) {
                Logger.TraceLog($"SubProvider is activated. Find {nameof(ResourceSubProviderAttribute)}...", Color.yellow);
                _subProvider = Init(providerTypeList.Where(x => x.IsDefined<ResourceSubProviderAttribute>()).OrderBy(x => x.GetCustomAttribute<ResourceSubProviderAttribute>().priority));
                if (_subProvider == null) {
                    Logger.TraceError($"{nameof(_subProvider)} is Null. Check {nameof(ResourceSubProviderAttribute)} Implementation. Temporarily create {nameof(NullResourceProvider)}");
                    _subProvider = new NullResourceProvider();
                    // _subProvider.Init();
                }
            }
            
            _provider = Init(providerTypeList.Where(x => x.IsDefined<ResourceProviderAttribute>()).OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>().priority));
        } catch (Exception ex) {
            Logger.TraceError(ex);
            if (_provider == null) {
                Logger.TraceLog($"Provider is Invalid. Temporarily create {nameof(NullResourceProvider)}");
                _provider = new NullResourceProvider();
                // _provider.Init();
            }
        }
    }

    private IResourceProvider Init(IEnumerable<Type> enumerable) {
        foreach (var type in enumerable) {
            if (Activator.CreateInstance(type) is IResourceProvider provider && provider.Valid()) {
                // provider.Init();
                Logger.TraceLog($"Activate Provider || {type.Name} || {type.Name}", Color.cyan);
                return provider;
            }
        }
        
        Logger.TraceError($"Failed to find a valid provider. Check {nameof(IResourceProvider)}.{nameof(IResourceProvider.Valid)} Method Implementation");
        
        var nullProvider = new NullResourceProvider();
        // nullProvider.Init();
        return nullProvider;
    }

    void IService.Start() {
        if (_isActiveSubProvider) {
            _subProvider.Init();
            _subCacheProvider.Init();
        }

        _provider.Init();
        _cacheProvider.Init();

        _isServing = true;
    }

    void IService.Stop() { }

    void IService.Remove() {
        if (_isActiveSubProvider) {
            _subProvider.Unload();
        }
        
        _provider.Unload();

        _isServing = false;
    }

    public void Load(ResourceProviderOrder order) {
        if (order is ResourceSubProviderOrder && _isActiveSubProvider) {
            _subProvider.Load(order);
        } else {
            _provider.Load(order);
        }
    }

    public void Unload(ResourceProviderOrder order) {
        if (order is ResourceSubProviderOrder && _isActiveSubProvider) {
            // _subProvider.Unload();
            _subCacheProvider.Unload(order);
        } else {
            // _provider.Unload();
            _cacheProvider.Unload(order);
        }
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
public class ResourceProviderAttribute : PriorityAttribute {

    public ResourceProviderAttribute(uint priority) : base(priority) { }
}

[AttributeUsage(AttributeTargets.Class)]
public class ResourceSubProviderAttribute : PriorityAttribute {

    public ResourceSubProviderAttribute(uint priority) : base(priority) { }
}

public abstract record ResourceProviderOrder {
    
    public bool IsSub() => this is ResourceSubProviderOrder;
}
public abstract record ResourceSubProviderOrder : ResourceProviderOrder;