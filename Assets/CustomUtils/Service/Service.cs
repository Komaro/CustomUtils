using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;

public interface IService {
    
    void Init() { }
    void Start();
    void Stop();
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

    public static bool StartService(Type type) => StartService(GetService(type));
    public static bool StartService<TService>() where TService : class, IService, new() => StartService(GetService<TService>());
    
    public static bool StartService(E_SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => StartService(x));
        return targetTypeList.Count > 0;
    }

    private static bool StartService(IService service) {
        try {
            if (service == null) return false;
            service.Start();
            Logger.TraceLog($"Service Start || {service.GetType().Name}", Color.cyan);
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Start Failed || {service?.GetType().Name}");
            return false;
        }
    }

    public static bool StopService(Type type) => StopService(GetService(type));
    public static bool StopService<TService>() where TService : class, IService, new() => StopService(GetService<TService>());
    
    public static bool StopService(E_SERVICE_TYPE serviceType) {
        var targetTypeList = GetTypeList(serviceType);
        targetTypeList.ForEach(x => StopService(x));
        return targetTypeList.Count > 0;
    }
    
    public static bool StopService(IService service) {
        try {
            if (service == null) return false;
            service.Stop();
            Logger.TraceLog($"Service Stop || {service.GetType().Name}", Color.magenta);
            return true;
        } catch (Exception e) {
            Logger.TraceError(e);
            Logger.TraceError($"Service Stop Failed || {service?.GetType().Name}");
            return false;
        }
    }
    
    public static IService GetService(Type type) {
        if (_interfaceType.IsAssignableFrom(type) && _serviceDic.TryGetValue(type, out var iService) == false) {
            if (Activator.CreateInstance(type) is IService service) {
                service.Init();
                _serviceDic.Add(type, service);
            } else {
                Logger.TraceError($"{nameof(IService)} is Missing || GetType = {type.Name}");
                return null;
            }
        }

        return _serviceDic[type];
    }
    
    public static TService GetService<TService>() where TService : class, IService, new() {
        var type = typeof(TService);
        if (_serviceDic.TryGetValue(type, out var iService) == false) {
            var service = new TService();
            service.Init();
            _serviceDic.Add(type, service);
        }
        
        return _serviceDic[type] as TService;
    }
    
    public static bool RemoveService<TService>() where TService : class, IService => RemoveService(typeof(TService));

    public static bool RemoveService(E_SERVICE_TYPE type) {
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
        
        //Logger.TraceLog($"Remove Service Missing || {type.Name}", Color.red);
        return false;
    }
    
    public static List<IService> GetServiceList(E_SERVICE_TYPE type) => _serviceDic.Values.Where(x => x.GetType().GetCustomAttribute<ServiceAttribute>()?.serviceTypes.Contains(type) ?? false).ToList();
    public static List<Type> GetTypeList(E_SERVICE_TYPE serviceType) => _cachedServiceTypeList.FindAll(x => x.GetCustomAttribute<ServiceAttribute>()?.serviceTypes.Contains(serviceType) ?? false);
}

public enum E_SERVICE_TYPE {
    NONE,                 // 일반적으로 서비스 쵱초 Get 을 통해 서비스 시작
    GAME_SCENE_START,     // GameScene 시작과 동시에 서비스
    START_SCENE_START,    // StartScene 시작과 동시에 서비스 
}

public sealed class ServiceAttribute : Attribute {

    public E_SERVICE_TYPE[] serviceTypes;

    public ServiceAttribute() {
        serviceTypes = new[] { E_SERVICE_TYPE.NONE };
    }
    
    public ServiceAttribute(params E_SERVICE_TYPE[] serviceType) {
        this.serviceTypes = serviceType;
    }
}
