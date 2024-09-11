using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

[ResourceProvider(100)]
public class AssetBundleProvider : IResourceProvider {

    private Dictionary<string, AssetBundle> _cacheAssetBundleDic = new();
    private Dictionary<string, (string path, AssetBundle assetBundle)> _cacheAssetPathDic = new();

    private Dictionary<string, Object> _cacheAssetDic = new();

    private bool _isLoaded;
    
    public bool Valid() {
        if (Service.TryGetService<CachingService>(out var service)) {
            var cache = service.Get();
            if (cache.ready && cache.valid && Directory.Exists(cache.path)) {
                return true;
            }
        }

        return false;
    }

    public void Init() {
        _cacheAssetBundleDic.SafeClear(x => x.Unload(true));
        _cacheAssetDic.Clear();
        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles()) {
            _cacheAssetBundleDic.AutoAdd(bundle.name, bundle);
        }
    }

    public void Load(ResourceProviderOrder order) {
        throw new System.NotImplementedException();
    }

    public void Unload() {
        throw new System.NotImplementedException();
    }

    public void Unload(ResourceProviderOrder order) {
        throw new System.NotImplementedException();
    }

    public void Load() {
        // TODO. TestCode
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            // TODO. Local Process
        } else {
            var resource = Service.GetService<ResourceService>();
            if (resource.TryGet<TextAsset>("AssetBundleChecksumInfo", out var asset) && asset.dataSize > 0 && asset.text.TryToJson<AssetBundleChecksumInfo>(out var info)) {
                var manifest = LoadManifest(info);
                if (manifest != null) {
                    LoadAssetBundle(info);
                }
            }
        }
        
        _isLoaded = true;
    }

    private AssetBundleManifest LoadManifest(AssetBundleChecksumInfo info) {
        if (info.crcDic.TryGetValue(info.target, out var crc)) {
            var assetBundle = AssetBundle.LoadFromFile($"C:/Project/Unity/CustomUtils/AssetBundle/{info.target}/{info.target}", crc);
            if (assetBundle != null && assetBundle.TryFindManifest(out var manifest)) {
                return manifest;
            }
        }
        
        return null;
    }

    private void LoadAssetBundle(AssetBundleChecksumInfo info) {
        foreach (var name in info.hashDic.Keys) {
            if (info.TryGetChecksum(name, out var checkSum)) {
                var assetBundle = AssetBundle.LoadFromFile($"C:/Project/Unity/CustomUtils/AssetBundle/{info.target}/{name}", checkSum.crc);
                if (assetBundle != null) {
                    _cacheAssetBundleDic.AutoAdd(assetBundle.name, assetBundle);
                    foreach (var path in assetBundle.GetAllAssetNames()) {
                        var assetName = Path.GetFileNameWithoutExtension(path);
                        _cacheAssetPathDic.AutoAdd(assetName.ToUpper(), (path, assetBundle));
                    }
                }
            }
        }
    }

    private async Task<AssetBundleManifest> ManifestDownloadAsync(AssetBundleChecksumInfo info) {
        if (info.crcDic.TryGetValue(info.target, out var crc)) {
            var task = Service.GetService<DownloadService>().DownloadAsync(new AssetBundleManifestDownloadHandler($"http://localhost:8000/{info.target}/{info.target}", crc));
            var handler = await task;
            if (task.IsCompletedSuccessfully) {
                return handler.GetContent();
            }
        }

        return null;
    }

    private async Task AssetBundleDownloadAsync(AssetBundleChecksumInfo info) {
        foreach (var name in info.hashDic.Keys) {
            if (info.TryGetChecksum(name, out var checkSum)) {
                var task = Service.GetService<DownloadService>().DownloadAssetBundleAsync($"http://localhost:8000/{info.target}/{name}", checkSum.hash, checkSum.crc);
                var handler = await task;
                if (task.IsCompletedSuccessfully && handler != null) {
                    var assetBundle = handler.assetBundle;
                    if (assetBundle != null) {
                        Logger.TraceLog($"{assetBundle.name}");
                        _cacheAssetBundleDic.AutoAdd(assetBundle.name, assetBundle);
                    }
                }
            }
        }
    }

    public void Unload(IDictionary<string, Object> cacheResource) {
        cacheResource.Clear();
        _cacheAssetBundleDic.Clear();
        AssetBundle.UnloadAllAssetBundles(true);
    }

    public void UnloadAssetBundle(string key) {
        if (_cacheAssetBundleDic.TryGetValue(key, out var assetBundle)) {
            assetBundle.Unload(true);
            _cacheAssetBundleDic.Remove(key);
        }
    }

    public Object Get(string name) {
        name = name.ToUpper();
        if (_cacheAssetDic.TryGetValue(name, out var ob)) {
            return ob;
        }

        if (_cacheAssetPathDic.TryGetValue(name, out var info)) {
            ob = info.assetBundle.LoadAsset(info.path);
            if (ob != null) {
                _cacheAssetDic.AutoAdd(name, ob);
                return ob;
            }
        }

        return null;
    }
    
    public string GetPath(string name) => _cacheAssetPathDic.TryGetValue(name.ToUpper(), out var info) ? info.path : string.Empty;

    public bool IsLoaded() => _isLoaded;
    public bool IsNull() => false;
}
