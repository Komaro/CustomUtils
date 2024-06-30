using UnityEngine;

public static class AssetBundleExtension {

    private const string MANIFEST = "manifest";
    
    public static bool TryFindManifest(this AssetBundle assetBundle, out AssetBundleManifest manifest) {
        manifest = assetBundle.FineManifest();
        return manifest != null;
    }

    public static AssetBundleManifest FineManifest(this AssetBundle assetBundle) {
        foreach (var assetBundleName in assetBundle.GetAllAssetNames()) {
            if (assetBundleName.Contains(MANIFEST)) {
                var manifest = assetBundle.LoadAsset<AssetBundleManifest>(assetBundleName);
                return manifest;
            }
        }

        return null;
    }
}
