using System;
using UnityEngine;

public static partial class Service {

    public static async Awaitable<TService> GetServiceAsync<TService>() where TService : class, IService, new() => await GetServiceAsync(typeof(TService)) as TService;

    public static async Awaitable<IService> GetServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        if (_interfaceType.IsAssignableFrom(type) == false) {
            Logger.TraceError($"{type.FullName} is Not Assignable from {_interfaceType.FullName}");
            return null;
        }

        if (_serviceDic.ContainsKey(type) == false) {
            var service = await CreateServiceAsync(type);
            if (service == null || await StartServiceAsync(service) == false) {
                return null;
            }
        }

        return _serviceDic[type];
    }

    public static async Awaitable<bool> StartServiceAsync(IService service) {
        await Awaitable.MainThreadAsync();
        return StartService(service);
    }

    public static async Awaitable<IService> CreateServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return CreateService(type);
    }
}
