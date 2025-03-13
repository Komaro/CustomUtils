using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR
using UnityEditor;
#endif

public static partial class Service {

    private static ImmutableHashSet<Type> _cachedServiceTypeSet;
    private static readonly Dictionary<Type, IService> _serviceDic = new ();

    private static bool _isInitialized = false;
    
#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR
    
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorApplication.isPlayingOrWillChangePlaymode == false) {
            Initialize();
        }
    }
    
#endif
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize() {
        if (_isInitialized == false) {
            _cachedServiceTypeSet = ReflectionProvider.GetClasses().Where(type => type.IsAbstract == false && typeof(IService).IsAssignableFrom(type) && type.Name.StartsWith("Sample_") == false).ToImmutableHashSetWithDistinct();
            _isInitialized = true;
        }
    }
    

    #region [Start]

    public static bool StartService(Enum serviceType) => TryGetTypes(serviceType, out var types) && types.All(StartService);
    public static bool StartService(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && types.All(StartService);
    public static bool StartService<TService>() where TService : class, IService, new() => StartService(typeof(TService));

    public static bool StartService(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) == false || TryCreateService(type, out service) == false) {
            return false;
        }

        return StartService(service);
    }

    private static bool StartService(IService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }

            if (service.IsServing() == false) {
                service.Start();
                Logger.TraceLog($"{nameof(StartService)} || {service.GetType().Name}", Color.cyan);
            } else {
                Logger.TraceLog($"{service.GetType().Name} is already serving", Color.yellow);    
            }
            
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(StartService)} failed || {service?.GetType().Name}");
            return false;
        }
    }
    
    #endregion

    #region [Stop]
    
    public static bool StopService(Enum serviceType) => TryGetTypes(serviceType, out var types) && types.All(StopService);
    public static bool StopService(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && types.All(StopService);
    public static bool StopService<TService>() where TService : class, IService, new() => StopService(typeof(TService));

    public static bool StopService(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) == false) {
            Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
            return false;
        }
        
        return StopService(service);
    }

    public static bool StopService(IService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            service.Stop();
            Logger.TraceLog($"{nameof(StopService)} || {service.GetType().Name}", Color.magenta);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(StopService)} failed || {service?.GetType().Name}");
            return false;
        }
    }
    
    #endregion

    #region [Restart]
    
    public static bool RestartService(Enum serviceType) => TryGetTypes(serviceType, out var types) && types.All(RestartService);
    public static bool RestartService(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && types.All(RestartService);
    public static bool RestartService<TService>() where TService : class, IService, new() => RestartService(typeof(TService));
    
    public static bool RestartService(Type type) {
        if (TryGetService(type, out var service) == false) {
            Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
            return false;
        }

        return RestartService(service);
    }

    public static bool RestartService(IService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            service.Stop();
            service.Start();
            Logger.TraceLog($"{nameof(RestartService)} || {service.GetType().Name}", Color.green);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(RestartService)} failed || {service?.GetType().Name}");
            return false;
        }
    }

    #endregion

    #region [Refresh]
   
    public static bool RefreshService(Enum serviceType) => TryGetTypes(serviceType, out var types) && types.All(RefreshService);
    public static bool RefreshService(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && types.All(RefreshService);
    public static bool RefreshService<TService>() where TService : class, IService, new() => RefreshService(typeof(TService));
    
    public static bool RefreshService(Type type) {
        if (TryGetService(type, out var service) == false) {
            Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
            return false;
        }

        return RefreshService(service);
    }

    public static bool RefreshService(IService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            service.Refresh();
            Logger.TraceLog($"{nameof(RefreshService)} || {service.GetType().Name}", Color.yellow);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(RefreshService)} Failed || {service?.GetType().Name}");
            return false;
        }
    }

    #endregion
    
    #region [Get]
    
    public static bool TryGetServiceWithRestart<TService>(out TService service) where TService : class, IService, new() => TryGetService(out service) && RestartService(service);
    
    public static bool TryGetService(Enum serviceType, out IEnumerable<IService> services) => (services = GetService(serviceType)).Any();
    public static IEnumerable<IService> GetService(Enum serviceType) => GetTypes(serviceType).Select(GetService);
    
    public static bool TryGetService(out IEnumerable<IService> services, params Enum[] serviceTypes) => (services = GetService(serviceTypes)).Any();
    public static IEnumerable<IService> GetService(params Enum[] serviceTypes) => GetTypes(serviceTypes).Select(GetService);
    
    public static bool TryGetService<TService>(out TService service) where TService : class, IService, new() => (service = GetService<TService>()) != null;
    public static TService GetService<TService>() where TService : class, IService, new() => GetService(typeof(TService)) as TService;

    public static bool TryGetService<TService>(Type type, out TService service) where TService : class, IService, new() => (service = GetService<TService>(type)) != null;
    public static TService GetService<TService>(Type type) where TService : class, IService, new() => GetService(type) as TService;

    public static bool TryGetService(Type type, out IService service) => (service = GetService(type)) != null;
    public static IService GetService(Type type) => _serviceDic.TryGetValue(type, out var service) ? service : TryCreateService(type, out service) && StartService(service) ? service : null;

    #endregion

    #region [Create]
    
    private static bool TryCreateService<TService>(out TService service) where TService : class, IService, new() => (service = CreateService<TService>()) != null;
    private static TService CreateService<TService>() where TService : class, IService, new() => CreateService(typeof(TService)) as TService;

    private static bool TryCreateService<TService>(Type type, out TService service) where TService : class, IService, new() => (service = CreateService(type) as TService) != null;
    private static bool TryCreateService(Type type, out IService service) => (service = CreateService(type)) != null;

    private static IService CreateService(Type type) {
        if (typeof(IService).IsAssignableFrom(type) == false) {
            Logger.TraceError($"{type.FullName} is not assignable from {typeof(IService).FullName}");
            return null;
        }
        
        if (_cachedServiceTypeSet.Contains(type) && SystemUtil.TryCreateInstance<IService>(out var service, type)) {
            _serviceDic.Add(type, service);
            service.Init();
            return service;
        }

        Logger.TraceError($"{nameof(CreateService)} failed || {type.Name}");
        return null;
    }
    
    #endregion

    #region [Remove]
    
    public static bool RemoveService(Enum serviceType) => TryGetTypes(serviceType, out var types) && types.All(RemoveService);
    public static bool RemoveService(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && types.All(RemoveService);
    public static bool RemoveService<TService>() where TService : class, IService => RemoveService(typeof(TService));

    public static bool RemoveService(Type type) {
        try {
            if (_serviceDic.TryGetValue(type, out var service) == false) {
                Logger.TraceLog($"{type.Name} {nameof(Service)} not running", Color.yellow);
                return false;
            }
            
            service.Stop();
            service.Remove();
            Logger.TraceLog($"{nameof(RemoveService)} || {type.Name}", Color.yellow);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(RemoveService)} Failed || {type.Name}");
            return false;
        }
    }

    #endregion
    
    public static bool TryGetRunningServices(Enum serviceType, out IEnumerable<IService> services) => (services = GetRunningServices(serviceType))?.Any() ?? false;
    public static IEnumerable<IService> GetRunningServices(Enum serviceType) => _serviceDic.Where(pair => ContainsType(pair.Key, serviceType)).ToValues();

    public static bool TryGetRunningServices(out IEnumerable<IService> services, params Enum[] serviceTypes) => (services = GetRunningServices(serviceTypes))?.Any() ?? false;
    public static IEnumerable<IService> GetRunningServices(params Enum[] serviceTypes) => _serviceDic.Where(pair => ContainsType(pair.Key, serviceTypes)).ToValues();
    
    public static bool TryGetRunningServices<TEnum>(TEnum serviceType, out IEnumerable<IService> services) where TEnum : struct, Enum => (services = GetRunningServices(serviceType))?.Any() ?? false;
    public static IEnumerable<IService> GetRunningServices<TEnum>(TEnum serviceType) where TEnum : struct, Enum => _serviceDic.Where(pair => ContainsType(pair.Key, serviceType)).ToValues();

    public static bool TryGetRunningServices<TEnum>(out IEnumerable<IService> services, params TEnum[] serviceTypes) where TEnum : struct, Enum => (services = GetRunningServices(serviceTypes))?.Any() ?? false;
    public static IEnumerable<IService> GetRunningServices<TEnum>(params TEnum[] serviceTypes) where TEnum : struct, Enum => _serviceDic.Where(pair => ContainsType(pair.Key, serviceTypes)).ToValues();
    
    public static bool TryGetTypes(Enum serviceType, out IEnumerable<Type> types) => (types = GetTypes(serviceType))?.Any() ?? false;
    public static IEnumerable<Type> GetTypes(Enum serviceType) => _cachedServiceTypeSet == null || serviceType == null ? Enumerable.Empty<Type>() : _cachedServiceTypeSet.Where(type => ContainsType(type, serviceType));

    public static bool TryGetTypes(out IEnumerable<Type> types, params Enum[] serviceTypes) => (types = GetTypes(serviceTypes))?.Any() ?? false;
    public static IEnumerable<Type> GetTypes(params Enum[] serviceTypes) => _cachedServiceTypeSet == null || serviceTypes.IsEmpty() ? Enumerable.Empty<Type>() : _cachedServiceTypeSet.Where(type => ContainsType(type, serviceTypes));

    public static bool TryGetTypes<TEnum>(TEnum serviceType, out IEnumerable<Type> types) where TEnum : struct, Enum => (types = GetTypes(serviceType))?.Any() ?? false;
    public static IEnumerable<Type> GetTypes<TEnum>(TEnum serviceType) where TEnum : struct, Enum => _cachedServiceTypeSet?.Where(type => ContainsType(type, serviceType)) ?? Enumerable.Empty<Type>();

    public static bool TryGetTypes<TEnum>(out IEnumerable<Type> types, params TEnum[] serviceTypes) where TEnum : struct, Enum => (types = GetTypes(serviceTypes))?.Any() ?? false;
    public static IEnumerable<Type> GetTypes<TEnum>(params TEnum[] serviceTypes) where TEnum : struct, Enum => _cachedServiceTypeSet?.Where(type => ContainsType(type, serviceTypes)) ?? Enumerable.Empty<Type>();
    
    private static bool ContainsType(Type type, Enum serviceType) => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceType);
    private static bool ContainsType(Type type, params Enum[] serviceTypes) => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceTypes);
    private static bool ContainsType<TEnum>(Type type, TEnum serviceType) where TEnum : struct, Enum => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceType);
    private static bool ContainsType<TEnum>(Type type, params TEnum[] serviceTypes) where TEnum : struct, Enum => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceTypes);
}

public enum DEFAULT_SERVICE_TYPE {
    NONE,
    START_MAIN_THREAD,
    RESOURCE_LOAD,
    PLAY_DURING,
    PLAY_FOCUS_DURING,
    PLAY_DURING_AFTER_INIT,
}