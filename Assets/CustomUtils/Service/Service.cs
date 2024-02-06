using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

public interface IService {
    
    bool IsServing() => false;
    void Init() { }
    void Start();
    void Stop();
    void Refresh() { }
    void Remove() { }
}

public static class Service {

    private static List<Type> _cachedServiceTypeList = new();
    private static ReactiveDictionary<Type, IService> _serviceDic = new ();
    
    private static readonly Type _attributeType = typeof(ServiceAttribute);
    private static readonly Type _interfaceType = typeof(IService);

    private static bool _isInitialized = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize() {
        if (_isInitialized == false) {
            _cachedServiceTypeList = ReflectionManager.GetInterfaceTypes<IService>().ToList();
            _isInitialized = true;
        }
    }
    
    public static bool StartService(Type type) => StartService(_serviceDic.TryGetValue(type, out var service) ? service : CreateService(type));
    public static bool StartService<TService>() where TService : class, IService, new() => StartService(_serviceDic.TryGetValue(typeof(TService), out var service) ? service : CreateService<TService>());

    public static bool StartService(SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => StartService(x));
        return targetTypeList.Count > 0;
    }

    private static bool StartService(IService service) {
        try {
            if (service == null) {
                return false;
            }

            if (service.IsServing() == false) {
                service.Start();
                Logger.TraceLog($"Service Start || {service.GetType().Name}", Color.cyan);
            }
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Start Failed || {service?.GetType().Name}");
            return false;
        }
    }

    public static bool StopService(Type type) => StopService(_serviceDic.TryGetValue(type, out var service) ? service : CreateService(type));
    public static bool StopService<TService>() where TService : class, IService, new() => StopService(_serviceDic.TryGetValue(typeof(TService), out var service) ? service : CreateService<TService>());

    public static bool StopService(SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => StopService(x));
        return targetTypeList.Count > 0;
    }
    
    public static bool StopService(IService service) {
        try {
            if (service == null) {
                return false;
            }
            
            service.Stop();
            Logger.TraceLog($"Service Stop || {service.GetType().Name}", Color.magenta);
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Stop Failed || {service?.GetType().Name}");
            return false;
        }
    }

    public static bool RestartService(Type type) => RestartService(_serviceDic.TryGetValue(type, out var service) ? service : CreateService(type));
    public static bool RestartService<TService>() where TService : class, IService, new() => RestartService(_serviceDic.TryGetValue(typeof(TService), out var service) ? service : CreateService<TService>());

    public static bool RestartService(SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => RestartService(x));
        return targetTypeList.Count > 0;
    }
    
    public static bool RestartService(IService service) {
        try {
            if (service == null) {
                return false;
            }
            
            service.Stop();
            service.Start();
            Logger.TraceLog($"Service Restart || {service.GetType().Name}", Color.green);
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Restart Failed || {service?.GetType().Name}");
            return false;
        }
    }

    public static bool RefreshService(Type type) => _serviceDic.TryGetValue(type, out var service) && RefreshService(service);
    public static bool RefreshService<TService>() where TService : class, IService, new() => _serviceDic.TryGetValue(typeof(TService), out var service) && RefreshService(service);

    public static bool RefreshService(SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => RefreshService(x));
        return targetTypeList.Count > 0;
    }

    public static bool RefreshService(IService service) {
        try {
            if (service == null) {
                return false;
            }
            
            service.Refresh();
            Logger.TraceLog($"Service Refresh || {service.GetType().Name}", Color.yellow);
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Refresh Failed || {service?.GetType().Name}");
            return false;
        }
    }

    public static bool TryGetService<TService>(out TService service) where TService : class, IService, new() {
        service = GetService<TService>();
        return service != null;
    }
    
    public static TService GetService<TService>() where TService : class, IService, new() => GetService(typeof(TService)) as TService;
    
    public static IService GetService(Type type) {
        if (_interfaceType.IsAssignableFrom(type) == false) {
            Logger.TraceError($"{type.FullName} is Not Assignable from {_interfaceType.FullName}");
            return null;
        }

        if (_serviceDic.ContainsKey(type) == false) {
            StartService(CreateService(type));
        }

        return _serviceDic[type];
    }
    
    private static IService CreateService(Type type) {
        if (Activator.CreateInstance(type) is IService service) {
            _serviceDic.Add(type, service);
            service.Init();
            return service;
        }
        
        return null;
    }

    private static IService CreateService<TService>() where TService : class, IService, new() => CreateService(typeof(TService));

    public static bool RemoveService<TService>() where TService : class, IService => RemoveService(typeof(TService));

    public static bool RemoveService(SERVICE_TYPE type) {
        var typeList = GetTypeList(type);
        return typeList.All(RemoveService);
    }
    
    public static bool RemoveService(Type type) {
        if (_serviceDic.TryGetValue(type, out var service)) {
            service.Stop();
            service.Remove();
            _serviceDic.Remove(type);
            return true;
        }
        
        return false;
    }
    
    public static List<IService> GetServiceList(SERVICE_TYPE type) => _serviceDic.Values.Where(x => x.GetType().GetCustomAttribute<ServiceAttribute>()?.serviceTypes.Contains(type) ?? false).ToList();
    public static List<Type> GetTypeList(SERVICE_TYPE serviceType) => _cachedServiceTypeList.FindAll(x => x.GetCustomAttribute<ServiceAttribute>()?.serviceTypes.Contains(serviceType) ?? false);
}

public enum SERVICE_TYPE {
    NONE,
    GAME_SCENE_DURING,           
    GAME_SCENE_DURING_AFTER_INIT,
    GAME_SCENE_FOCUS_DURING, 
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute {

    public SERVICE_TYPE[] serviceTypes;

    public ServiceAttribute() {
        serviceTypes = new[] { SERVICE_TYPE.NONE };
    }
    
    public ServiceAttribute(params SERVICE_TYPE[] serviceType) {
        this.serviceTypes = serviceType;
    }
}