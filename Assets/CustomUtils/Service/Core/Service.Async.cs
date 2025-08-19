using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ServiceOperation : AsyncCustomOperation {
    
    public ServiceOperation(bool isDone = false) {
        if (isDone) {
            Done();
        }
    }
}

public class ServiceBatchOperation : ServiceOperation {

    public override bool IsDone => _operations.Any(operation => operation.IsDone == false) == false;

    private readonly ServiceOperation[] _operations;

    public ServiceBatchOperation(IEnumerable<ServiceOperation> operations) : this(operations.ToArray()) { }

    public ServiceBatchOperation(params ServiceOperation[] operations) {
        operations.ThrowIfNull();
        _operations = operations;
        foreach (var operation in _operations) {
            operation.OnProgress += _ => OnReport();
        }
    }

    private void OnReport() => Report(_operations.Average(operation => operation.Progress));
}

public static partial class Service {

    public static ServiceOperation StartOperationService<TService>() where TService : IOperationService => StartOperationService(typeof(TService));
    public static ServiceOperation StartOperationService(Enum serviceType) => new ServiceBatchOperation(GetTypes(serviceType).Select(StartOperationService));

    private static ServiceOperation StartOperationService(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) == false || service is not IOperationService operationService) {
            return CreateAndStartOperationService(type);
        }

        return StartOperationService(operationService);
    }

    private static ServiceOperation CreateAndStartOperationService(Type type) {
        var createOperation = CreateOperationService(type);
        var startOperation = StartOperationService(type);
        var batchOperation = new ServiceBatchOperation(createOperation, startOperation);
        _ = CreateAnsStartOperationService(createOperation, startOperation);
        return batchOperation;
    }

    private static async Task CreateAnsStartOperationService(params ServiceOperation[] operations) {
        foreach (var operation in operations) {
            await operation.ToTask();
        }
    }
    
    private static ServiceOperation StartOperationService(IOperationService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return new ServiceOperation(true);
            }

            if (service.IsServing() == false) {
                var operation = service.StartOperation();
                Logger.TraceLog($"{nameof(StartServiceAsync)} || {service.GetType().Name}", Color.cyan);
                return operation;
            }

            Logger.TraceLog($"{service.GetType().Name} is already serving", Color.yellow);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(StartServiceAsync)} failed || {service?.GetType().Name}");
        }
        
        return new ServiceOperation(true);
    }
    
    private static ServiceOperation CreateOperationService<TService>() where TService : class, IOperationService, new() => CreateOperationService(typeof(TService));

    private static ServiceOperation CreateOperationService(Type type) {
        if (typeof(IOperationService).IsAssignableFrom(type) == false) {
            Logger.TraceError($"{type.FullName} is not assignable from {typeof(IOperationService).FullName}");
            return new ServiceOperation(true);
        }
        
        if (_cachedServiceTypeSet.Contains(type) && SystemUtil.TryCreateInstance<IOperationService>(out var service, type)) {
            _serviceDic.Add(type, service);
            return service.InitOperation();
        }

        Logger.TraceError($"{nameof(CreateOperationService)} failed || {type.Name}");
        return new ServiceOperation(true);
    }
    
    #region [StartAsync]
    
    public static async Task<bool> StartServiceAsync(Enum serviceType) => TryGetTypes(serviceType, out var types) && (await Task.WhenAll(types.Select(StartServiceAsync))).All(result => result);
    public static async Task<bool> StartServiceAsync(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && (await Task.WhenAll(types.Select(StartServiceAsync))).All(result => result);
    public static async Task<bool> StartServiceAsync<TService>() where TService : class, IAsyncService, new() => await StartServiceAsync(typeof(TService));
    
    public static async Task<bool> StartServiceAsync(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) == false) {
            service = await CreateServiceAsync(type);
        }

        return service is IAsyncService asyncService && await StartServiceAsync(asyncService);
    }

    private static async Task<bool> StartServiceAsync(IAsyncService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }

            if (service.IsServing() == false) {
                await service.StartAsync();
                Logger.TraceLog($"{nameof(StartServiceAsync)} || {service.GetType().Name}", Color.cyan);
            } else {
                Logger.TraceLog($"{service.GetType().Name} is already serving", Color.yellow);    
            }
            
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(StartServiceAsync)} failed || {service?.GetType().Name}");
            return false;
        }
    }
    
    #endregion
    
    #region [StopAsync]
    
    public static async Task<bool> StopServiceAsync(Enum serviceType) => TryGetTypes(serviceType, out var types) && (await Task.WhenAll(types.Select(StopServiceAsync))).All(result => result);
    public static async Task<bool> StopServiceAsync(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && (await Task.WhenAll(types.Select(StopServiceAsync))).All(result => result);
    public static async Task<bool> StopServiceAsync<TService>() where TService : class, IAsyncService, new() => await StopServiceAsync(typeof(TService));
    
    public static async Task<bool> StopServiceAsync(Type type) {
        if (_serviceDic.TryGetValue(type, out var service) == false) {
            Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
            return false;
        }
        
        if (service is IAsyncService asyncService) {
            return await StopServiceAsync(asyncService);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not implement {nameof(IAsyncService)}", Color.yellow);
        return false;
    }

    public static async Task<bool> StopServiceAsync(IAsyncService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            await service.StopAsync();
            Logger.TraceLog($"{nameof(StopServiceAsync)} || {service.GetType().Name}", Color.magenta);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(StopServiceAsync)} failed || {service?.GetType().Name}");
            return false;
        }
    }
    
    #endregion

    #region [RestartAsync]
    
    public static async Task<bool> RestartServiceAsync(Enum serviceType) => TryGetTypes(serviceType, out var types) && (await Task.WhenAll(types.Select(RestartServiceAsync))).All(result => result);
    public static async Task<bool> RestartServiceAsync(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && (await Task.WhenAll(types.Select(RestartServiceAsync))).All(result => result);
    public static async Task<bool> RestartServiceAsync<TService>() where TService : class, IAsyncService => await RestartServiceAsync(typeof(TService));

    public static async Task<bool> RestartServiceAsync(Type type) {
        if (TryGetService(type, out var service) && service is IAsyncService asyncService) {
            return await RestartServiceAsync(asyncService);
        }

        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
        return false;
    }

    public static async Task<bool> RestartServiceAsync(IAsyncService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            await service.StopAsync();
            await service.StartAsync();
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
   
    public static async Task<bool> RefreshServiceAsync(Enum serviceType) => TryGetTypes(serviceType, out var types) && (await Task.WhenAll(types.Select(RefreshServiceAsync))).All(result => result);
    public static async Task<bool> RefreshServiceAsync(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && (await Task.WhenAll(types.Select(RefreshServiceAsync))).All(result => result);
    public static async Task<bool> RefreshServiceAsync<TService>() where TService : class, IAsyncService => await RefreshServiceAsync(typeof(TService));
    
    public static async Task<bool> RefreshServiceAsync(Type type) {
        if (TryGetService(type, out var service) && service is IAsyncService asyncService) {
            return await RefreshServiceAsync(asyncService);
        }
        
        Logger.TraceLog($"{type.Name} {nameof(Service)} is not ready", Color.yellow);
        return false;
    }

    public static async Task<bool> RefreshServiceAsync(IAsyncService service) {
        try {
            if (service == null) {
                Logger.TraceError($"{nameof(service)} is null");
                return false;
            }
            
            await service.RefreshAsync();
            Logger.TraceLog($"{nameof(RefreshServiceAsync)} || {service.GetType().Name}", Color.yellow);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Logger.TraceError($"{nameof(RefreshServiceAsync)} Failed || {service?.GetType().Name}");
            return false;
        }
    }

    #endregion

    #region [CreateAsync]
    
    private static async Task<TService> CreateServiceAsync<TService>() where TService : class, IAsyncService, new() => await CreateServiceAsync(typeof(TService)) as TService;

    private static async Task<IAsyncService> CreateServiceAsync(Type type) {
        if (typeof(IAsyncService).IsAssignableFrom(type) == false) {
            Logger.TraceError($"{type.FullName} is not assignable from {typeof(IAsyncService).FullName}");
            return null;
        }
        
        if (_cachedServiceTypeSet.Contains(type) && SystemUtil.TryCreateInstance<IAsyncService>(out var service, type)) {
            _serviceDic.Add(type, service);
            await service.InitAsync();
            return service;
        }

        Logger.TraceError($"{nameof(CreateServiceAsync)} failed || {type.Name}");
        return null;
    }
    
    #endregion
    
    #region [RemoveAsync]
    
    public static async Task<bool> RemoveServiceAsync(Enum serviceType) => TryGetTypes(out var types, serviceType) && (await Task.WhenAll(types.Select(RemoveServiceAsync))).All(result => result);
    public static async Task<bool> RemoveServiceAsync(params Enum[] serviceTypes) => TryGetTypes(out var types, serviceTypes) && (await Task.WhenAll(types.Select(RemoveServiceAsync))).All(result => result);
    public static async Task<bool> RemoveServiceAsync<TService>() where TService : class, IAsyncService => await RemoveServiceAsync(typeof(TService));

    public static async Task<bool> RemoveServiceAsync(Type type) {
        try {
            if (_serviceDic.TryGetValue(type, out var service) == false) {
                Logger.TraceLog($"{type.Name} {nameof(Service)} not running", Color.yellow);
                return false;
            }

            if (service is IAsyncService asyncService) {
                await asyncService.StopAsync();
                await asyncService.RemoveAsync();
                Logger.TraceLog($"{nameof(RemoveService)} || {type.Name}", Color.yellow);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        Logger.TraceError($"{nameof(RemoveServiceAsync)} Failed || {type.Name} is not {nameof(IAsyncService)}");
        return false;
    }

    #endregion
}