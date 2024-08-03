using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

public static class Service {

    private static List<Type> _cachedServiceTypeList = new();
    private static ReactiveDictionary<Type, IService> _serviceDic = new ();
    
    private static readonly Type _attributeType = typeof(ServiceAttribute);
    private static readonly Type _interfaceType = typeof(IService);

    private static bool _isInitialized = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize() {
        if (_isInitialized == false) {
            _cachedServiceTypeList = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.IsDynamic == false)
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.IsClass && type.IsAbstract == false && _interfaceType.IsAssignableFrom(type) && type.Name.StartsWith("Sample_") == false).ToList();
            
            _isInitialized = true;
        }
    }

    public static bool StartService(Type type) => StartService(_serviceDic.TryGetValue(type, out var service) ? service : CreateService(type));
    public static bool StartService<TService>() where TService : class, IService, new() => StartService(_serviceDic.TryGetValue(typeof(TService), out var service) ? service : CreateService<TService>());

    public static bool StartService(Enum serviceType) {
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
            } else {
                Logger.TraceLog($"{service.GetType().Name} is Already Serving", Color.yellow);    
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

    public static bool StopService(Enum serviceType) {
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

    public static bool RestartService(Enum serviceType) {
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

    public static bool RefreshService(Enum serviceType) {
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

    public static bool TryGetServiceWithStart<TService>(out TService service) where TService : class, IService, new() {
        if (TryGetService(out service)) {
            if (service.IsServing() == false) {
                service.Start();
            }
            return true;
        }

        return false;
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

    public static bool RemoveService(Enum serviceType) {
        var typeList = GetTypeList(serviceType);
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
    
    public static List<IService> GetServiceList(Enum serviceType) => _serviceDic.Values.Where(x => x.GetType().GetCustomAttribute<ServiceAttribute>()?.serviceTypeSet.Contains(serviceType) ?? false).ToList();
    public static List<Type> GetTypeList(Enum serviceType) => _cachedServiceTypeList.FindAll(x => x.GetCustomAttribute<ServiceAttribute>()?.serviceTypeSet.Contains(serviceType) ?? false);
}

public enum DEFAULT_SERVICE_TYPE {
    NONE,
    START_MAIN_THREAD,
    RESOURCE_LOAD,
    PLAY_DURING,
    PLAY_FOCUS_DURING,
    PLAY_DURING_AFTER_INIT,
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute {

    public HashSet<Enum> serviceTypeSet = new();

    public ServiceAttribute() {
        serviceTypeSet.Add(DEFAULT_SERVICE_TYPE.NONE);
    }

    public ServiceAttribute(params object[] serviceTypes) {
        foreach (var type in serviceTypes) {
            if (type is Enum enumType) {
                serviceTypeSet.Add(enumType);
            }
        }
    }
}