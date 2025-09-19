using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Color = System.Drawing.Color;

#if UNITY_EDITOR

using UnityEditor;

// TODO. Temp
public static partial class ServiceEx {

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorApplication.isPlayingOrWillChangePlaymode == false) {
            Initialize();
        }
    }
}

#endif


[RefactoringRequired("각 기능 별 IEnumerable 처리의 경우 bool 값을 리턴하기 위해 All 처리를 하는데 이 경우 조건에 맞지 않는 경우가 생기면 나머지 작업을 처리하지 않게 됨." +
                     "이를 보완하기 위해 전체적인 설계및 bool 리턴 기준을 조정할 필요가 있음")]
public static partial class ServiceEx {
    
    private static ImmutableHashSet<Type> _cachedServiceTypeSet;
    private static readonly Dictionary<Type, IService> _serviceDic = new ();

    private static bool _isInitialized;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize() {
        if (_isInitialized == false) {
            _cachedServiceTypeSet = ReflectionProvider.GetClassTypes().Where(type => type.IsAbstract == false && typeof(IService).IsAssignableFrom(type) && type.IsDefined<ObsoleteAttribute>() == false).ToImmutableHashSetWithDistinct();
            _isInitialized = true;
        }
    }

    #region  [Start]
    
    public static bool Start<TEnum>(TEnum serviceType) where TEnum : Enum => Start(GetTypes(serviceType));
    public static bool Start(Enum serviceType) => Start(GetTypes(serviceType));
    public static bool Start<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => Start(GetTypes(serviceTypes));
    public static bool Start(params Enum[] serviceTypes) => Start(GetTypes(serviceTypes));

    public static bool Start(IEnumerable<Type> types) => types.Select(Start).All();

    public static bool Start<TService>() where TService : class, IService => Start(typeof(TService));

    public static bool Start(Type type) {
        if (TryGet(type, out var service)) {
            return Start(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }
    
    private static bool Start(IService service) {
        try {
            service.ThrowIfNull();
            if (service.IsServing() == false) {
                service.Start();
                Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} start", Color.DeepSkyBlue);
            } else {
                Logger.TraceLog($"{service.GetType().Name} is already serving", Color.Yellow);    
            }
            
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    #endregion

    #region [Stop]

    public static bool Stop(Enum serviceType) => Stop(GetTypes(serviceType));
    public static bool Stop<TEnum>(TEnum serviceType) where TEnum : Enum => Stop(GetTypes(serviceType));
    
    public static bool Stop<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => Stop(GetTypes(serviceTypes));
    public static bool Stop(params Enum[] serviceTypes) => Stop(GetTypes(serviceTypes));
    
    public static bool Stop(IEnumerable<Type> types) => types.Select(Stop).All();
    
    public static bool Stop<TService>() where TService : class, IService, new() => Stop(typeof(TService));

    public static bool Stop(Type type) {
        if (TryGet(type, out var service)) {
            return Stop(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }

    private static bool Stop(IService service) {
        try {
            service.ThrowIfNull();
            service.Stop();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} stop", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }
    
    #endregion
    
    #region [Restart]

    public static bool Restart<TEnum>(TEnum serviceType) where TEnum : Enum => Restart(GetTypes(serviceType));
    public static bool Restart(Enum serviceType) => Restart(GetTypes(serviceType));
    
    public static bool Restart<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => Restart(GetTypes(serviceTypes));
    public static bool Restart(params Enum[] serviceTypes) => Restart(GetTypes(serviceTypes));

    private static bool Restart(IEnumerable<Type> types) => types.Select(Restart).All();
    
    public static bool Restart<TService>() where TService : class, IService => Restart(typeof(TService));
    
    public static bool Restart(Type type) {
        if (TryGet(type, out var service)) {
            return Restart(service);
        }
    
        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }

    private static bool Restart(IService service) {
        try {
            service.ThrowIfNull();
            service.Stop();
            service.Start();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} restart", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }

    #endregion
    
    #region [Refresh]

    public static bool Refresh<TEnum>(TEnum serviceType) where TEnum : Enum => Refresh(GetTypes(serviceType));
    public static bool Refresh(Enum serviceType) => Refresh(GetTypes(serviceType));

    public static bool Refresh<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => Refresh(GetTypes(serviceTypes));
    public static bool Refresh(params Enum[] serviceTypes) => Refresh(GetTypes(serviceTypes));

    public static bool Refresh(IEnumerable<Type> types) => types.Select(Refresh).All();
    
    public static bool Refresh<TService>() where TService : class, IService => Refresh(typeof(TService));
    
    public static bool Refresh(Type type) {
        if (TryGet(type, out var service)) {
            return Refresh(service);
        }
        
        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }

    private static bool Refresh(IService service) {
        try {
            service.ThrowIfNull();
            service.Refresh();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} refresh", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }

    #endregion

    #region [Get]

    public static bool TryGet<TService>(out TService service) where TService : class, IService => (service = Get<TService>()) != null;
    public static TService Get<TService>() where TService : class, IService => Get(typeof(TService)) as TService;

    public static bool TryGet<TService>(Type type, out TService service) where TService : class, IService => (service = Get(type) as TService) != null;
    public static TService Get<TService>(Type type) where TService : class, IService => Get(type) as TService;
    
    public static bool TryGet(Type type, out IService service) => (service = Get(type)) != null;

    public static IService Get(Type type) {
        if (_serviceDic.TryGetValue(type, out var service)) {
            return service;
        }

        return TryCreate(type, out service) == false ? null : Init(service);
    }
    
    #endregion

    #region [Init]
    
    private static IService Init(IService service) {
        service.ThrowIfNull();
        service.Init();
        Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} init", Color.Cyan);
        return service;
    }
    
    #endregion

    #region [Create]
    
    private static bool TryCreate<TService>(out TService service) where TService : class, IService => (service = Create<TService>()) != null;
    private static TService Create<TService>() where TService : class, IService => Create(typeof(TService)) as TService;

    private static bool TryCreate(Type type, out IService service) => (service = Create(type)) != null;

    private static IService Create(Type type) {
        type.ThrowIfInvalidCast<IService>();

        if (_cachedServiceTypeSet.Contains(type) == false || SystemUtil.TrySafeCreateInstance<IService>(out var service, type) == false) {
            throw new NotSupportedException($"{nameof(Create)} failed. {type.Name} is not support");
        }
        
        Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} create", Color.DeepSkyBlue);
        return _serviceDic.ReturnAdd(type, service);
    }

    #endregion

    #region [Remove]
    
    public static bool Remove<TEnum>(TEnum serviceType) where TEnum : Enum => Remove(GetTypes(serviceType));
    public static bool Remove(Enum serviceType) => Remove(GetTypes(serviceType));

    public static bool Remove<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => Remove(GetTypes(serviceTypes));
    public static bool Remove(params Enum[] serviceTypes) => Remove(GetTypes(serviceTypes));
    
    public static bool Remove(IEnumerable<Type> types) => types.Select(Remove).All();
    
    public static bool Remove<TService>() where TService : class, IService => Remove(typeof(TService));

    public static bool Remove(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) != false) {
            return Remove(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} not running", Color.Yellow);
        return false;
    }

    private static bool Remove(IService service) {
        try {
            Stop(service);
            service.Remove();
            
            _serviceDic.Remove(service.GetType());
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} remove", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
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
    
    public static IEnumerable<Type> GetTypes(Enum serviceType) => _cachedServiceTypeSet == null || serviceType == null ? Enumerable.Empty<Type>() : _cachedServiceTypeSet.Where(type => ContainsType(type, serviceType));
    public static IEnumerable<Type> GetTypes(params Enum[] serviceTypes) => _cachedServiceTypeSet == null || serviceTypes.IsEmpty() ? Enumerable.Empty<Type>() : _cachedServiceTypeSet.Where(type => ContainsType(type, serviceTypes));
    public static IEnumerable<Type> GetTypes<TEnum>(TEnum serviceType) where TEnum : Enum => _cachedServiceTypeSet?.Where(type => ContainsType(type, serviceType)) ?? Enumerable.Empty<Type>();
    public static IEnumerable<Type> GetTypes<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => _cachedServiceTypeSet?.Where(type => ContainsType(type, serviceTypes)) ?? Enumerable.Empty<Type>();
    
    private static bool ContainsType(Type type, Enum serviceType) => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceType);
    private static bool ContainsType(Type type, params Enum[] serviceTypes) => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceTypes);
    private static bool ContainsType<TEnum>(Type type, TEnum serviceType) where TEnum : Enum => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceType);
    private static bool ContainsType<TEnum>(Type type, params TEnum[] serviceTypes) where TEnum : Enum => type.TryGetCustomAttribute<ServiceAttribute>(out var attribute) && attribute.Contains(serviceTypes);

    public static bool IsActiveService<TService>() => _serviceDic.ContainsKey(typeof(TService));
}

public static partial class ServiceEx {

    #region [StartAsync]
    
    public static async Task<bool> StartAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await StartAsync(GetTypes(serviceType));
    public static async Task<bool> StartAsync(Enum serviceType) => await StartAsync(GetTypes(serviceType));
    
    public static async Task<bool> StartAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await StartAsync(GetTypes(serviceTypes));
    public static async Task<bool> StartAsync(params Enum[] serviceTypes) => await StartAsync(GetTypes(serviceTypes));
    
    private static async Task<bool> StartAsync(IEnumerable<Type> types) => (await Task.WhenAll(types.Select(StartAsync))).All();
    
    public static async Task<bool> StartAsync<TService>() where TService : IService => await StartAsync(typeof(TService));
    
    public static async Task<bool> StartAsync(Type type) {
        var service = await GetAsync(type);
        if (service != null) {
            return await StartAsync(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }
    
    private static async Task<bool> StartAsync(IAsyncService service) {
        try {
            if (service.IsServing() == false) {
                await service.StartAsync();
                Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} async start", Color.DeepSkyBlue);
            } else {
                Logger.TraceLog($"{service.GetType().Name} is already serving", Color.Yellow);    
            }
            
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    #endregion

    #region [StopAsync]
    
    public static async Task<bool> StopAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await StopAsync(GetTypes(serviceType));
    public static async Task<bool> StopAsync(Enum serviceType) => await StopAsync(GetTypes(serviceType));

    public static async Task<bool> StopAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await StopAsync(GetTypes(serviceTypes));
    public static async Task<bool> StopAsync(params Enum[] serviceTypes) => await StopAsync(GetTypes(serviceTypes));

    private static async Task<bool> StopAsync(IEnumerable<Type> types) => (await Task.WhenAll(types.Select(StopAsync))).All();

    public static async Task<bool> StopAsync<TService>() where TService : class, IService, new() => await StopAsync(typeof(TService));
    
    public static async Task<bool> StopAsync(Type type) {
        var service = await GetAsync(type);
        if (service != null) {
            return await StopAsync(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }
    
    private static async Task<bool> StopAsync(IAsyncService service) {
        try {
            await service.StopAsync();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} async stop", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }
    
    #endregion

    #region [RestartAsync]

    public static async Task<bool> RestartAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await RestartAsync(GetTypes(serviceType));
    public static async Task<bool> RestartAsync(Enum serviceType) => await RestartAsync(GetTypes(serviceType));
    
    public static async Task<bool> RestartAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await RestartAsync(GetTypes(serviceTypes));
    public static async Task<bool> RestartAsync(params Enum[] serviceTypes) => await RestartAsync(GetTypes(serviceTypes));

    private static async Task<bool> RestartAsync(IEnumerable<Type> types) => (await Task.WhenAll(types.Select(RestartAsync))).All();
    
    public static async Task<bool> RestartAsync<TService>() where TService : class, IService => await RestartAsync(typeof(TService));
    
    public static async Task<bool> RestartAsync(Type type) {
        var service = await GetAsync(type);
        if (service != null) {
            return await RestartAsync(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }

    private static async Task<bool> RestartAsync(IAsyncService service) {
        try {
            await service.StopAsync();
            await service.StartAsync();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} async restart", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    #endregion

    #region [RefreshAsync]
    
    public static async Task<bool> RefreshAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await RefreshAsync(GetTypes(serviceType));
    public static async Task<bool> RefreshAsync(Enum serviceType) => await RefreshAsync(GetTypes(serviceType));
    
    public static async Task<bool> RefreshAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await RefreshAsync(GetTypes(serviceTypes));
    public static async Task<bool> RefreshAsync(params Enum[] serviceTypes) => await RefreshAsync(GetTypes(serviceTypes));
    
    public static async Task<bool> RefreshAsync(IEnumerable<Type> types) => (await Task.WhenAll(types.Select(RefreshAsync))).All();

    
    public static async Task<bool> RefreshAsync<TService>() where TService : class, IService => await RefreshAsync(typeof(TService));
    
    public static async Task<bool> RefreshAsync(Type type) {
        var service = await GetAsync(type);
        if (service != null) {
            return await RefreshAsync(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
        return false;
    }

    private static async Task<bool> RefreshAsync(IAsyncService service) {
        try {
            await service.RefreshAsync();
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} async refresh", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    #endregion

    #region [GetAsync]
    
    public static async Task<IEnumerable<IService>> GetAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await Task.WhenAll(GetTypes(serviceType).Select(GetAsync));
    public static async Task<IEnumerable<IService>> GetAsync(Enum serviceType) => await Task.WhenAll(GetTypes(serviceType).Select(GetAsync));
    
    public static async Task<IEnumerable<IService>> GetAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await Task.WhenAll(GetTypes(serviceTypes).Select(GetAsync));
    public static async Task<IEnumerable<IService>> GetAsync(params Enum[] serviceTypes) => await Task.WhenAll(GetTypes(serviceTypes).Select(GetAsync));
    
    public static async Task<TService> GetAsync<TService>() where TService : class, IService => await GetAsync(typeof(TService)) as TService;
    public static async Task<TService> GetAsync<TService>(Type type) where TService : class, IService => await GetAsync(type) as TService;
    
    public static async Task<IAsyncService> GetAsync(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) && service is IAsyncService asyncService) {
            return asyncService;
        }

        return TryCreate(type, out service) ? await InitAsync(service as IAsyncService) : null;
    }
    
    #endregion

    #region [InitAsync]
    
    private static async Task<IAsyncService> InitAsync(IAsyncService service) {
        service.ThrowIfNull();
        await service.InitAsync();
        return service;
    }
    
    #endregion

    #region [RemoveAsync]

    public static async Task<bool> RemoveAsync<TEnum>(TEnum serviceType) where TEnum : Enum => await RemoveAsync(GetTypes(serviceType));
    public static async Task<bool> RemoveAsync(Enum serviceType) => await RemoveAsync(GetTypes(serviceType));
    
    public static async Task<bool> RemoveAsync<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => await RemoveAsync(GetTypes(serviceTypes));
    public static async Task<bool> RemoveAsync(params Enum[] serviceTypes) => await RemoveAsync(GetTypes(serviceTypes));

    public static async Task<bool> RemoveAsync(IEnumerable<Type> types) => (await Task.WhenAll(types.Select(RemoveAsync))).All();

    public static async Task<bool> RemoveAsync<TService>() where TService : class, IService => await RemoveAsync(typeof(TService));

    public static async Task<bool> RemoveAsync(Type type) {
        var service = await GetAsync(type);
        if (service != null) {
            return await RemoveAsync(service);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} not running", Color.Yellow);
        return false;
    }

    private static async Task<bool> RemoveAsync(IAsyncService service) {
        try {
            await StopAsync(service);
            await service.RemoveAsync();
            
            _serviceDic.Remove(service.GetType());
            Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} remove", Color.DeepSkyBlue);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    #endregion
}

// Operation
// public static partial class ServiceEx {
//
//     private static readonly ServiceOperation DONE_OPERATION;
//     private static readonly ServiceOperation CANCEL_OPERATION;
//
//     static ServiceEx() {
//         DONE_OPERATION = new ServiceOperation();
//         DONE_OPERATION.Done();
//         
//         CANCEL_OPERATION = new ServiceOperation();
//         CANCEL_OPERATION.Cancel();
//     }
//
//     private static ServiceOperation StartAsyncOperation(Type type) {
//         if (_serviceDic.TryGetValue(type, out var service) && service is IAsyncService asyncService) {
//             try {
//                 if (service.IsServing() == false) {
//                     var operation = StartAsyncOperation(asyncService);
//                     operation.OnComplete += _ => Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} async operation start", Color.DeepSkyBlue);
//                     operation.LateStart();
//                     return operation;
//                 }
//
//                 Logger.TraceLog($"{service.GetType().Name} is already serving", Color.Yellow);
//                 return DONE_OPERATION;
//             } catch (Exception) {
//                 return CANCEL_OPERATION;
//             }
//         }
//         
//         var sequentiallyOperation = new ServiceSequentiallyOperation();
//         sequentiallyOperation.Append(GetAsyncOperation(type));
//         sequentiallyOperation.Append(StartAsyncOperation(type));
//         sequentiallyOperation.LateStart();
//         return sequentiallyOperation;
//     }
//     
//     private static ServiceOperation StartAsyncOperation(IAsyncService service) => new(operation => service.StartAsync(operation));
//
//     public static ServiceOperation StopAsyncOperation(Type type) {
//         if (_serviceDic.TryGetValue(type, out var service) && service is IAsyncService asyncService) {
//             try {
//                 var operation = StopAsyncOperation(asyncService);
//                 operation.OnComplete += _ => Logger.TraceLog($"{service.GetType().Name} {nameof(Service)} stop", Color.DeepSkyBlue);
//                 operation.LateStart();
//                 return operation;
//             } catch (Exception) {
//                 return CANCEL_OPERATION;
//             }
//         }
//
//         Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.LightGray);
//         return DONE_OPERATION;
//     }
//     
//     private static ServiceOperation StopAsyncOperation(IAsyncService service) => new(operation => service.StopAsync(operation));
//     
//     private static ServiceOperation GetAsyncOperation(Type type) {
//         if (_serviceDic.TryGetValue(type, out var service) && service is IAsyncService) {
//             return DONE_OPERATION;
//         }
//
//         if (TryCreate(type, out service) && service is IAsyncService asyncService) {
//             var operation = InitAsyncOperation(asyncService);
//             return operation;
//         }
//
//         return CANCEL_OPERATION;
//     }
//     
//     private static ServiceOperation InitAsyncOperation(IAsyncService service) => new(operation => _ = service.InitAsync(operation));
// }
//
// public class ServiceOperation : AsyncCustomOperation {
//
//     private bool _isCanceled;
//     public override bool IsCanceled => _isCanceled;
//
//     private readonly Action<ServiceOperation> _onStart;
//
//     public ServiceOperation() { }
//     public ServiceOperation(Action<ServiceOperation> onStart) => _onStart = onStart;
//
//     public void Cancel() => _isCanceled = true;
//
//     public virtual Task LateStart() {
//         var operation = new ServiceOperation();
//         operation.OnProgress += Report;
//         _onStart?.Invoke(operation);
//         return Task.CompletedTask;
//     }
// }
//
// public class ServiceSequentiallyOperation : ServiceOperation {
//
//     public ServiceSequentiallyOperation() : base(null) { }
//     public ServiceSequentiallyOperation(Action<ServiceOperation> onStart) : base(onStart) { }
//
//     public override float Progress => _operationList.Average(operation => operation.Progress);
//     public override bool IsDone => _operationList.All(operation => operation.IsDone);
//
//     private readonly List<ServiceOperation> _operationList = new();
//     public void Append(ServiceOperation operation) {
//         _operationList.Add(operation);
//         operation.OnProgress += _ => OnReport();
//     }
//
//     public override async Task LateStart() {
//         foreach (var operation in _operationList) {
//             await operation.LateStart();
//         }
//     }
//     
//     private void OnReport() => Report(_operationList.Average(operation => operation.Progress));
// }

// public class ServiceBatchOperation : ServiceOperation {
//
//     public override float Progress => _operations.Average(operation => operation.Progress);
//     public override bool IsDone => _operations.All(operation => operation.IsDone);
//
//     private readonly ServiceOperation[] _operations;
//
//     // public ServiceBatchOperation(IEnumerable<ServiceOperation> operations) : this(operations.ToArray()) { }
//
//     // public ServiceBatchOperation(params ServiceOperation[] operations) {
//     //     operations.ThrowIfNull();
//     //     _operations = operations;
//     //     foreach (var operation in _operations) {
//     //         operation.OnProgress += _ => OnReport();
//     //     }
//     // }
//
//     private void OnReport() => Report(_operations.Average(operation => operation.Progress));
// }