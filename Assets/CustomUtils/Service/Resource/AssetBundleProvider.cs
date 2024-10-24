using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// Temp Provider
[ResourceProvider(1)]
public class AssetBundleProvider : IResourceProvider {

    private AssetBundleChecksumInfo _checksumInfo;
    private ConcurrentDictionary<string, AssetBundleWrapperBase> _bundleDic = new();
    private ConcurrentDictionary<string, string> _assetToBundleDic = new();

    private const string UNITY_3D_EXTENSION = ".unity3d";
    
    // Temp Init
    public void Init() {
        if (Service.GetService<ResourceService>().TryGet<TextAsset>("AssetBundleChecksumInfo", out var asset) && asset.dataSize > 0 && asset.text.TryToJson(out _checksumInfo)) {
            if (_checksumInfo.crcDic.TryGetValue(_checksumInfo.target, out var crc) && AssetBundleUtil.TryLoadManifestFromFile(out var manifest, $"{Constants.Path.PROJECT_PATH}/AssetBundle/{_checksumInfo.target}/{_checksumInfo.target}", crc)) {
                foreach (var name in manifest.GetAllAssetBundles().Where(name => string.IsNullOrEmpty(name) == false)) {
                    foreach (var type in ReflectionProvider.GetSubClassTypes<AssetBundleWrapperBase>().OrderBy(type => type.GetOrderByPriority())) {
                        if (type.IsDefined<OnlyEditorEnvironmentAttribute>() && Application.isEditor == false) {
                            continue;
                        }

                        if (SystemUtil.TryCreateInstance<AssetBundleWrapperBase>(out var wrapper, type, name, _checksumInfo) && wrapper.IsReady()) {
                            _bundleDic.TryAdd(name, wrapper);
                            break;
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

    public TOrder ExecuteOrder<TOrder>(TOrder order) where TOrder : ResourceOrder => order;

    public void Load(ResourceOrder order) {
        switch (order) {
            case AssetBundleLoadAsyncOrder loadAsyncOrder:
                Service.GetService<UnityMainThreadDispatcherService>().Enqueue(ProgressAsync(GetLoadRequests(loadAsyncOrder.assetBundles).WhereNotNull().ToList(), loadAsyncOrder.callback));
                break;
            case AssetBundleLoadOrder loadOrder:
                foreach (var name in loadOrder.assetBundles) {
                    if (_bundleDic.TryGetValue(name.AutoSwitchExtension(UNITY_3D_EXTENSION).ToLower(), out var wrapper)) {
                        wrapper.Load();
                        foreach (var key in wrapper.GetAssetKeys()) {
                            _assetToBundleDic.TryAdd(key, wrapper.name);
                        }
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

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class AssetBundleWrapperBase {
    
    public readonly string name;
    protected readonly AssetBundleChecksumInfo checksumInfo;

    public AssetBundleWrapperBase(string name, AssetBundleChecksumInfo checksumInfo) {
        this.name = name;
        this.checksumInfo = checksumInfo;
    }

    public abstract void Clear();
    
    public abstract void Load();
    public abstract AssetBundleCreateRequest LoadAsync();
    
    public abstract void Unload();
    public abstract AssetBundleUnloadOperation UnloadAsync();

    public abstract Object Get(string assetName);
    public abstract string GetPath(string assetName);

    protected abstract void RefreshPath();

    public abstract IEnumerable<string> GetAssetKeys();
    
    public abstract bool IsReady();
    public abstract bool IsValid();
}

[Priority(0)]
[OnlyEditorEnvironment]
public class AssetBundleWrapper : AssetBundleWrapperBase {
    
    private AssetBundle assetBundle;
    private readonly ConcurrentDictionary<string, string> _assetPathDic = new();
    private readonly ConcurrentDictionary<string, Object> _assetCacheDic = new();

    public AssetBundleWrapper(string name, AssetBundleChecksumInfo checksumInfo) : base(name, checksumInfo) { }

    public override void Clear() {
        Unload();
        _assetPathDic.Clear();
    }

    public override void Load() {
        if (assetBundle == null && string.IsNullOrEmpty(name) == false) {
            assetBundle = AssetBundle.LoadFromFile($"{Constants.Path.PROJECT_PATH}/AssetBundle/{checksumInfo.target}/{name}");
            if (assetBundle == null) {
                Logger.TraceError($"{nameof(assetBundle)} is null");
                return;
            }
            
            if (_assetPathDic.IsEmpty) {
                RefreshPath();
            }
        }
    }

    public override AssetBundleCreateRequest LoadAsync() {
        if (assetBundle == null && string.IsNullOrEmpty(name) == false) {
            var request = AssetBundle.LoadFromFileAsync($"{Constants.Path.PROJECT_PATH}/AssetBundle/{checksumInfo.target}/{name}");
            request.completed += _ => {
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

    public override void Unload() {
        if (assetBundle != null) {
            _assetCacheDic.Clear();
            assetBundle.Unload(true);
        }
    }

    public override AssetBundleUnloadOperation UnloadAsync() {
        if (assetBundle != null) {
            _assetCacheDic.Clear();
            return assetBundle.UnloadAsync(true);
        }
        
        return null;
    }

    public override Object Get(string assetName) {
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

    public override string GetPath(string assetName) => _assetPathDic.TryGetValue(assetName, out var path) ? path : string.Empty;

    protected override void RefreshPath() {
        if (assetBundle == null) {
            Logger.TraceError($"{nameof(assetBundle)} is null");
            return;
        }
        
        foreach (var fullPath in assetBundle.GetAllAssetNames()) {
            var assetName = Path.GetFileNameWithoutExtension(fullPath).ToUpper();
            _assetPathDic.TryAdd(assetName, fullPath);
        }
    }
    
    public override IEnumerable<string> GetAssetKeys() => _assetPathDic.Keys;

    public override bool IsReady() => string.IsNullOrEmpty(name) == false && checksumInfo != null;
    public override bool IsValid() => assetBundle != null;
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