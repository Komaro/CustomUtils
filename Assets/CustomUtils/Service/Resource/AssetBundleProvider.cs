using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[ResourceProvider(1)]
public class AssetBundlePovider : IResourceProvider {

    private AssetBundleChecksumInfo _checksumInfo;
    private ConcurrentDictionary<string, AssetBundleWrapper> _bundleDic = new();
    private ConcurrentDictionary<string, string> _assetToBundleDic = new();

    private const string UNITY_3D_EXTENSION = ".unity3d";
    
    public void Init() {
        if (Service.GetService<ResourceService>().TryGet<TextAsset>("AssetBundleChecksumInfo", out var asset) && asset.dataSize > 0 && asset.text.TryToJson(out _checksumInfo)) {
            if (_checksumInfo.crcDic.TryGetValue(_checksumInfo.target, out var crc)) {
                var assetBundle = AssetBundle.LoadFromFile($"{Constants.Path.PROJECT_PATH}/AssetBundle/{_checksumInfo.target}/{_checksumInfo.target}", crc);
                if (assetBundle != null && assetBundle.TryFindManifest(out var manifest)) {
                    foreach (var name in manifest.GetAllAssetBundles().Where(name => string.IsNullOrEmpty(name) == false)) {
                        if (_checksumInfo.TryGetChecksum(name, out var info)) {
                            // TODO. AssetBundleWrapper dynamic instantiate
                            var wrapper = new AssetBundleWrapper(name, _checksumInfo);
                            _bundleDic.TryAdd(name, wrapper);
                            foreach (var key in wrapper.GetAssetKeys()) {
                                _assetToBundleDic.TryAdd(key, name);
                            }
                        }
                    }
                }
            }
        }
    }

    public void Clear() {
        _checksumInfo = null;
        _bundleDic.SafeClear(wrapper => wrapper.Clear());
        _assetToBundleDic.Clear();
    }

    public void ExecuteOrder(ResourceOrder order) {
        
    }

    public void Load(ResourceOrder order) {
        switch (order) {
            case AssetBundleLoadAsyncOrder loadAsyncOrder:
                Service.GetService<UnityMainThreadDispatcherService>().Enqueue(ProgressAsync(GetLoadRequests(loadAsyncOrder.assetBundles).WhereNotNull().ToList(), loadAsyncOrder.callback));
                break;
            case AssetBundleLoadOrder loadOrder:
                foreach (var name in loadOrder.assetBundles) {
                    if (_bundleDic.TryGetValue(name.AutoSwitchExtension(UNITY_3D_EXTENSION).ToLower(), out var wrapper)) {
                        wrapper.Load();
                    }
                }
                break;
        }
    }
    
    public void Unload(ResourceOrder order) {
        switch (order) {
            case AssetBundleUnloadAsyncOrder unloadAsyncOrder:
                Service.GetService<UnityMainThreadDispatcherService>().Enqueue(ProgressAsync(GetUnloadOperations(unloadAsyncOrder.assetBundles).WhereNotNull().ToList(), unloadAsyncOrder.callback));
                break;
            case AssetBundleUnloadOrder unloadOrder:
                foreach (var name in unloadOrder.assetBundles) {
                    if (_bundleDic.TryGetValue(name.AutoSwitchExtension(UNITY_3D_EXTENSION).ToLower(), out var wrapper)) {
                        wrapper.Unload();
                    }
                }
                break;
        }
    }

    private IEnumerable<AssetBundleCreateRequest> GetLoadRequests(IEnumerable<string> names) { 
        foreach (var name in names) {
            if (_bundleDic.TryGetValue(name.AutoSwitchExtension(UNITY_3D_EXTENSION).ToLower(), out var wrapper)) {
                yield return wrapper.LoadAsync();
            }
        }
    }

    private IEnumerable<AssetBundleUnloadOperation> GetUnloadOperations(IEnumerable<string> names) {
        foreach (var name in names) {
            if (_bundleDic.TryGetValue(name.AutoSwitchExtension(UNITY_3D_EXTENSION).ToLower(), out var wrapper)) {
                yield return wrapper.UnloadAsync();
            }
        }
    }

    private IEnumerator ProgressAsync(IReadOnlyCollection<AsyncOperation> operations, Action<float> callback = null) {
        while (operations.Any(request => request.isDone == false)) {
            callback?.Invoke(operations.Sum(request => request.progress));
            yield return null;
        }
        
        callback?.Invoke(operations.Sum(request => request.progress));
    }

    public Object Get(string name) {
        name = name.ToUpper();
        if (_assetToBundleDic.TryGetValue(name, out var bundleName) && _bundleDic.TryGetValue(bundleName, out var wrapper)) {
           return wrapper.Get(name);
        }

        return null;
    }

    public Object Get(ResourceOrder order) {
        switch (order) {
            case AssetBundleOrder assetBundle:
                return Get(assetBundle.assetName);
        }
        
        return null;
    }

    public string GetPath(string name) {
        name = name.ToUpper();
        if (_assetToBundleDic.TryGetValue(name, out var bundleName) && _bundleDic.TryGetValue(bundleName, out var wrapper)) {
            return wrapper.GetPath(name);
        }

        return string.Empty;
    }

    public  bool IsReady() => AssetBundle.GetAllLoadedAssetBundles().Any() == false;
    public bool IsNull() => false;
}

// TODO. Wrapper to module
public abstract class AssetBundleWrapperBase {
    
    public readonly string name;
    private readonly AssetBundleChecksumInfo _checksumInfo;

    public AssetBundleWrapperBase(string name, AssetBundleChecksumInfo checksumInfo) {
        this.name = name;
        _checksumInfo = checksumInfo;
    }
}

internal class AssetBundleWrapper {

    public readonly string name;
    private readonly AssetBundleChecksumInfo _checksumInfo;
    
    private AssetBundle assetBundle;
    private readonly ConcurrentDictionary<string, string> _assetPathDic = new();
    private readonly ConcurrentDictionary<string, Object> _assetCacheDic = new();

    public AssetBundleWrapper(string name, AssetBundleChecksumInfo checksumInfo) {
        this.name = name;
        _checksumInfo = checksumInfo;
    }

    public void Clear() {
        Unload();
        _assetPathDic.Clear();
    }

    public void Load() {
        if (assetBundle == null && string.IsNullOrEmpty(name) == false) {
            assetBundle = AssetBundle.LoadFromFile($"{Constants.Path.PROJECT_PATH}/AssetBundle/{_checksumInfo.target}/{name}");
            if (assetBundle == null) {
                Logger.TraceError($"{nameof(assetBundle)} is null");
                return;
            }
            
            if (_assetPathDic.IsEmpty) {
                RefreshPath();
            }
        }
    }

    public AssetBundleCreateRequest LoadAsync() {
        if (assetBundle == null && string.IsNullOrEmpty(name) == false) {
            var request = AssetBundle.LoadFromFileAsync($"{Constants.Path.PROJECT_PATH}/AssetBundle/{_checksumInfo.target}/{name}");
            request.completed += operation => {
                if (request.assetBundle == null) {
                    Logger.TraceError($"{nameof(assetBundle)} is null");
                    return;
                }
                
                if (_assetPathDic.IsEmpty) {
                    RefreshPath();
                }
            };

            return request;
        }
        
        return null;
    }

    public void Unload() {
        if (assetBundle != null) {
            _assetCacheDic.Clear();
            assetBundle.Unload(true);
        }
    }

    public AssetBundleUnloadOperation UnloadAsync() {
        if (assetBundle != null) {
            _assetCacheDic.Clear();
            return assetBundle.UnloadAsync(true);
        }
        
        return null;
    }

    public Object Get(string assetName) {
        if (assetBundle == null) {
            Logger.TraceError($"{name} {nameof(AssetBundle)} is null");
            return null;
        }

        if (_assetCacheDic.TryGetValue(assetName, out var ob)) {
            return ob;
        }

        if (_assetPathDic.TryGetValue(assetName, out var path)) {
            ob = assetBundle.LoadAsset(path);
            if (ob != null) {
                _assetCacheDic.TryAdd(assetName, ob);
                return ob;
            }
        }
        
        return null;
    }

    public string GetPath(string assetName) => _assetPathDic.TryGetValue(assetName, out var path) ? path : string.Empty;

    private void RefreshPath() {
        if (assetBundle == null) {
            Logger.TraceError($"{nameof(assetBundle)} is null");
            return;
        }
        
        foreach (var fullPath in assetBundle.GetAllAssetNames()) {
            var assetName = Path.GetFileNameWithoutExtension(fullPath).ToUpper();
            _assetPathDic.TryAdd(assetName, fullPath);
        }
    }

    public IEnumerable<string> GetAssetKeys() => _assetPathDic.Keys;

    public bool IsValid() => assetBundle != null;
}


public record AssetBundleOrder : ResourceOrder {
    
    public readonly string assetName;

    public AssetBundleOrder(string assetName) => this.assetName = assetName;
}

public record AssetBundleLoadOrder : ResourceOrder {

    public string[] assetBundles;
}

public record AssetBundleLoadAsyncOrder : AssetBundleLoadOrder {

    public Action<float> callback;
}

public record AssetBundleUnloadOrder : ResourceOrder {
    
    public string[] assetBundles;
}

public record AssetBundleUnloadAsyncOrder : AssetBundleUnloadOrder {

    public Action<float> callback;
}