using System;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER

public static partial class Service {
    
    public static async Awaitable<bool> StartServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return StartService(type);
    }

    public static async Awaitable<bool> StartServiceAsync<TService>() where TService : class, IService, new() {
        await Awaitable.MainThreadAsync();
        return StartService<TService>();
    }

    public static async Awaitable<bool> StartServiceAsync(Enum serviceType) {
        await Awaitable.MainThreadAsync();
        return StartService(serviceType);
    }

    public static async Awaitable<bool> StopServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return StopService(type);
    }

    public static async Awaitable<bool> StopServiceAsync<TService>() where TService : class, IService, new() {
        await Awaitable.MainThreadAsync();
        return StopService<TService>();
    }
    
    public static async Awaitable<bool> StopServiceAsync(Enum serviceType) {
        await Awaitable.MainThreadAsync();
        return StopService(serviceType);
    }
    
    public static async Awaitable<bool> RestartServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return RestartService(type);
    }

    public static async Awaitable<bool> RestartServiceAsync<TService>() where TService : class, IService, new() {
        await Awaitable.MainThreadAsync();
        return RestartService<TService>();
    }
    
    public static async Awaitable<bool> RestartServiceAsync(Enum serviceType) {
        await Awaitable.MainThreadAsync();
        return RestartService(serviceType);
    }
    
    public static async Awaitable<bool> RefreshServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return RefreshService(type);
    }

    public static async Awaitable<bool> RefreshServiceAsync<TService>() where TService : class, IService, new() {
        await Awaitable.MainThreadAsync();
        return RefreshService<TService>();
    }
    
    public static async Awaitable<bool> RefreshServiceAsync(Enum serviceType) {
        await Awaitable.MainThreadAsync();
        return RefreshService(serviceType);
    }

    public static async Awaitable<TService> GetServiceAsync<TService>() where TService : class, IService, new() {
        await Awaitable.MainThreadAsync();
        return GetService<TService>();
    }
    
    public static async Awaitable<IService> GetServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return GetService(type);
    }
    
    
    public static async Awaitable<bool> RemoveServiceAsync<TService>() where TService : class, IService {
        await Awaitable.MainThreadAsync();
        return RemoveService<TService>();
    }

    public static async Awaitable<bool> RemoveServiceAsync(Enum serviceType) {
        await Awaitable.MainThreadAsync();
        return RemoveService(serviceType);
    }
    
    public static async Awaitable<bool> RemoveServiceAsync(Type type) {
        await Awaitable.MainThreadAsync();
        return RemoveService(type);
    }
}

#endif
