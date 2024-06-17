using System;
using System.Collections.Generic;
using UnityEngine;

public class CachingService : IService {
    
    void IService.Start() { }
    void IService.Stop() { }

    public Cache Add(string directoryName) {
        try {
            var path = $"{Application.persistentDataPath}/{directoryName}";
            var cache = Caching.GetCacheByPath(path);
            if (cache.valid) {
                Logger.TraceLog($"Already {nameof(Caching)} path || {path}", Color.yellow);
                return cache;
            }
            
            SystemUtil.EnsureDirectoryExists(path);
            Logger.TraceLog($"Add {nameof(Caching)} path || {path}", Color.yellow);
            return Caching.AddCache(path);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Caching.defaultCache;
    }

    public void Remove(string directoryName) {
        try {
            if (Caching.ready) {
                var cache = Caching.GetCacheByPath($"{Application.persistentDataPath}/{directoryName}");
                Remove(cache);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public void Remove(Cache cache) {
        if (cache.valid) {
            Logger.TraceLog($"Remove {nameof(Caching)} path || {cache.path}", Color.red);
            Caching.RemoveCache(cache);
        } else {
            Logger.TraceLog($"{nameof(Cache)} is already invalid.");
        }
    }

    public Cache Get(string directoryName, bool isCreate = true) {
        try {
            var cache = Caching.GetCacheByPath($"{Application.persistentDataPath}/{directoryName}");
            if (cache.valid == false && isCreate) {
                cache = Add(directoryName);
            }
            
            return cache;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Caching.defaultCache;
    }

    public Cache Get() => Caching.currentCacheForWriting;

    public Cache Set(string directoryName, bool isCreate = true) {
        try {
            var cache = Get(directoryName, isCreate);
            if (cache.valid == false && isCreate) {
                cache = Add(directoryName);
            }

            return Set(cache);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Caching.defaultCache;
    }

    public Cache Set(Cache cache) {
        if (cache.valid) {
            return Caching.currentCacheForWriting = cache;
        }
        
        Logger.TraceError($"{nameof(cache)} is Invalid. Return default {nameof(Cache)}");
        return Caching.defaultCache;
    }
    
    public bool Clear(string directoryName) => Clear(Get(directoryName));

    public bool Clear(Cache cache) {
        if (cache.valid) {
            Logger.TraceLog($"Clear {nameof(Caching)} path || {cache.path}", Color.red);
            return cache.ClearCache();
        }

        Logger.TraceLog($"{cache} is Invalid.");
        return false;
    }
    
    public bool Clear() => Caching.currentCacheForWriting.ClearCache();

    public List<Cache> GetAllCacheList() {
        try {
            var pathList = new List<string>();
            Caching.GetAllCachePaths(pathList);
            return pathList.ConvertAll(Caching.GetCacheByPath);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return new List<Cache>();
        }
    }

    public bool IsReady() => Caching.ready;
}