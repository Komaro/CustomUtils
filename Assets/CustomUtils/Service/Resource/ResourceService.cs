using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

[RequiresAttributeImplementation(typeof(ResourceProviderAttribute))]
public interface IResourceProvider : IImplementNullable {

    void Init();
    void Clear();
    TOrder ExecuteOrder<TOrder>(TOrder order) where TOrder : ResourceOrder;
    void Load(ResourceOrder order);
    void Unload(ResourceOrder order);
    Object Get(string name);
    Object Get(ResourceOrder order);
    string GetPath(string name);
    bool IsReady();
}

[Service(DEFAULT_SERVICE_TYPE.RESOURCE_LOAD)]
public class ResourceService : IService {

    private IResourceProvider _mainProvider;
    private IResourceProvider _subProvider;
    private readonly ConcurrentDictionary<Type, IResourceProvider> _extraProviderDic = new();

    private bool _isServing;
    private bool _isActiveSubProvider;
    private bool _isActiveExtraProvider;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        try {
            var grouping = ReflectionProvider.GetInterfaceTypes<IResourceProvider>().GroupBy(type => {
                if (type.IsDefined<ResourceExtraProviderAttribute>()) {
                    return typeof(ResourceExtraProviderAttribute);
                }

                return type.IsDefined<ResourceSubProviderAttribute>() ? typeof(ResourceSubProviderAttribute) : typeof(ResourceProviderAttribute);
            });
            
            var moduleDic = grouping.ToDictionary(group => group.Key, group => group.OrderBy(type => type.TryGetCustomAttribute<PriorityAttribute>(group.Key, out var attribute) ? attribute.priority : uint.MaxValue).ToList());
            if (moduleDic.TryGetValue(typeof(ResourceSubProviderAttribute), out var typeList) && typeList.Count > 0) {
                _isActiveSubProvider = true;
                _subProvider = GetValidProvider(typeList);
            }

            if (moduleDic.TryGetValue(typeof(ResourceProviderAttribute), out typeList)) {
                _mainProvider = GetValidProvider(typeList);
            }

            if (moduleDic.TryGetValue(typeof(ResourceExtraOrder), out typeList) && typeList.Count > 0) {
                _isActiveExtraProvider = true;
                foreach (var type in typeList) {
                    if (SystemUtil.TryCreateInstance<IResourceProvider>(out var module, type) && module.IsReady() && type.TryGetCustomAttribute<ResourceExtraProviderAttribute>(out var attribute) && attribute.orderType != null) {
                        _extraProviderDic.TryAdd(attribute.orderType, module);
                    }
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    void IService.Start() {
        if (_isActiveExtraProvider) {
            _extraProviderDic.Values.ForEach(module => module.Init());
        }
        
        if (_isActiveSubProvider) {
            _subProvider.Init();
        }
        
        _mainProvider.Init();

        _isServing = true;
    }

    void IService.Stop() { }

    void IService.Remove() {
        if (_isActiveExtraProvider) {
            _extraProviderDic.SafeClear(module => module.Clear());
        }
        
        if (_isActiveSubProvider) {
            _subProvider.Clear();
        }
        
        _mainProvider.Clear();
        
        _isServing = false;
    }
    
    public void ExecuteOrder(ResourceOrder order) => GetSwitchProvider(order).ExecuteOrder(order);
    public TOrder ExecuteOrder<TOrder>(TOrder order) where TOrder : ResourceOrder => GetSwitchProvider(order).ExecuteOrder(order);
    public void Load(ResourceOrder order) => GetSwitchProvider(order).Load(order);
    public void Unload(ResourceOrder order) => GetSwitchProvider(order).Unload(order);

    #region [Get]
    
    public bool TryGet(string name, out GameObject go) {
        go = Get(name);
        return go != null;
    }
    
    public GameObject Get(string name) => GetObject(name) as GameObject;

    public bool TryGet<T>(string name, out T obj) where T : Object {
        obj = Get<T>(name);
        return obj != null;
    }
    
    public T Get<T>(string name) where T : Object => GetObject(name) as T;

    public Object GetObject(string name) {
        var obj = _mainProvider?.Get(name) ?? _subProvider?.Get(name);
        if (obj == null) {
            Logger.TraceError($"{nameof(obj)} is null. missing resource || {name}");
            return default;
        }
        
        return obj;
    }

    public bool TryGet(ResourceOrder order, out GameObject go) => (go = Get(order)) != null;
    public GameObject Get(ResourceOrder order) => GetObject(order) as GameObject;
    
    public bool TryGet<T>(ResourceOrder order, out T obj) where T : Object => (obj = Get<T>(order)) != null;
    public T Get<T>(ResourceOrder order) where T : Object => GetObject(order) as T;

    public object GetObject(ResourceOrder order) {
        var ob = GetSwitchProvider(order).Get(order);
        if (ob == null) {
            Logger.TraceError($"{nameof(ob)} is null. missing resource || {order.GetType().Name}");
            return default;
        }

        return ob;
    }

    public bool TryGetPath(string name, out string path) {
        path = GetPath(name);
        return string.IsNullOrEmpty(path) == false;
    }

    public string GetPath(string name) => _mainProvider?.GetPath(name) ?? _subProvider?.GetPath(name);

    #endregion

    #region [Instantiate]

    public bool TryInstantiate<TComponent>(out TComponent component, string name, GameObject parent, bool isAddComponent = false) where TComponent : Component => (component = Instantiate<TComponent>(name, parent, isAddComponent)) != null;

    public TComponent Instantiate<TComponent>(string name, GameObject parent, bool isAddComponent = false) where TComponent : Component => Instantiate<TComponent>(name, parent.transform, isAddComponent);

    public bool TryInstantiate<TComponent>(out TComponent component, string name, Transform parent = null, bool isAddComponent = false) where TComponent : Component => (component = Instantiate<TComponent>(name, parent, isAddComponent)) != null;

    public TComponent Instantiate<TComponent>(string name, Transform parent = null, bool isAddComponent = false) where TComponent : Component {
        if (TryInstantiate(out var go, name, parent)) {
            return isAddComponent ? go.GetOrAddComponent<TComponent>() : go.GetComponent<TComponent>();
        }

        Logger.TraceError($"{nameof(go)} is null");
        return null;
    }

    public bool TryInstantiate<TComponent>(out TComponent component, string name, bool isAddComponent = false) where TComponent : Component => (component = Instantiate<TComponent>(name, isAddComponent)) != null;

    public TComponent Instantiate<TComponent>(string name, bool isAddComponent = false) where TComponent : Component {
        if (TryInstantiate(out var go, name)) {
            return isAddComponent ? go.GetOrAddComponent<TComponent>() : go.GetComponent<TComponent>();
        }

        Logger.TraceError($"{nameof(go)} is null");
        return null;
    }

    public bool TryInstantiate(out GameObject go, string name, Transform parent = null) => (go = Instantiate(name, parent)) != null;

    public GameObject Instantiate(string name, Transform parent = null) {
        var obj = GetObject(name);
        if (obj == null) {
            Logger.TraceError($"{nameof(obj)} is null");
            return null;
        }
        
        var go = Object.Instantiate(obj, parent) as GameObject;
        if (go == null) {
            Logger.TraceError($"{nameof(go)} is null");
            return null;
        }
        
        return go;
    }

    public bool TryInstantiate<T>(ResourceOrder order, GameObject parent, out T instant) where T : Object => (instant = Instantiate<T>(order, parent)) != null;
    public T Instantiate<T>(ResourceOrder order, GameObject parent) where T : Object => Instantiate<T>(order, parent.transform);

    public bool TryInstantiate<T>(out T instant, ResourceOrder order, Transform parent = null) where T : Object => (instant = Instantiate<T>(order, parent)) != null;

    public T Instantiate<T>(ResourceOrder order, Transform parent = null) where T : Object {
        if (TryGet<T>(order, out var obj) == false) {
            Logger.TraceError($"{nameof(obj)} is null");
            return null;
        }

        if (ObjectUtil.TryInstantiate(obj, parent, out var instant) == false) {
            Logger.TraceError($"{nameof(instant)} is null");
            return null;
        }

        return instant;
    }

    #endregion
    
    private IResourceProvider GetValidProvider(IEnumerable<Type> enumerable) {
        foreach (var type in enumerable) {
            if (SystemUtil.TryCreateInstance<IResourceProvider>(out var provider, type) && provider.IsReady()) {
                Logger.TraceLog($"Activate provider || {type.Name}", Color.cyan);
                return provider;
            }
        }
        
        Logger.TraceError($"Failed to find a valid module. Check {nameof(IResourceProvider)}.{nameof(IResourceProvider.IsReady)} Method Implementation. Returns the {nameof(NullResourceProvider)}");
        return new NullResourceProvider();
    }
    
    private IResourceProvider GetSwitchProvider(ResourceOrder order) {
        switch (order) {
            case ResourceSubOrder:
                if (_isActiveSubProvider) {
                    return _subProvider;
                }
                break;
            case ResourceExtraOrder:
                if (_isActiveExtraProvider && _extraProviderDic.TryGetValue(order.GetType(), out var extraProvider)) {
                    return extraProvider;
                }
                break;
        }
        
        return _mainProvider;
    }
    
    private IEnumerable<Type> GetFilteringModules<TAttribute>(IEnumerable<Type> enumerable) where TAttribute : PriorityAttribute => enumerable.Where(type => type.IsDefined<TAttribute>()).OrderBy(type => type.GetCustomAttribute<TAttribute>().priority);

}

[AttributeUsage(AttributeTargets.Class)]
public class ResourceProviderAttribute : PriorityAttribute {

    public ResourceProviderAttribute(uint priority) : base(priority) { }
}

[AttributeUsage(AttributeTargets.Class)]
public class ResourceSubProviderAttribute : PriorityAttribute {

    public ResourceSubProviderAttribute(uint priority) : base(priority) { }
}

[AttributeUsage(AttributeTargets.Class)]
public class ResourceExtraProviderAttribute : Attribute {

    public Type orderType;

    public ResourceExtraProviderAttribute(Type orderType) {
        if (orderType.IsSubclassOf(typeof(ResourceExtraOrder))) {
            this.orderType = orderType; 
        }
    }
}

public abstract record ResourceOrder {

    public bool IsMain() => IsSub() == false && IsExtra() == false;
    public bool IsSub() => this is ResourceSubOrder;
    public bool IsExtra() => this is ResourceExtraOrder;
}

public abstract record ResourceSubOrder : ResourceOrder;
public abstract record ResourceExtraOrder : ResourceOrder;