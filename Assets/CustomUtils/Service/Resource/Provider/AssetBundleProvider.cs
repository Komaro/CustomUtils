using System.Collections.Generic;
using UnityEngine;

[ResourceProvider(100)]
public class AssetBundleProvider : IResourceProvider {

    private Dictionary<string, AssetBundle> _cacheAssetBundleDic = new();

    public bool Valid() {
        
    
        return true; // Temp
    }

    public void Init() {
        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles()) {
            Logger.TraceError(bundle.name);
            _cacheAssetBundleDic.AutoAdd(bundle.name, bundle);
        }
    }

    public void Load() {
        // TODO. 에셋번들 로컬 다운로드 기능 추가 후 작업 시작.
    }

    public void Unload(Dictionary<string, Object> cacheResource) => _cacheAssetBundleDic.SafeClear(x => x.Unload(true));

    public Object Get(string name) => null;
    public string GetPath(string name) => string.Empty;
    public bool IsLoaded() => false;
}
