using UnityEngine;

public static class AssetBundleExtension {

    private const string MANIFEST = "manifest";
    
    public static bool TryGetManifest(this AssetBundle assetBundle, out AssetBundleManifest manifest) {
        manifest = assetBundle.GetManifest();
        return manifest != null;
    }

    public static AssetBundleManifest GetManifest(this AssetBundle assetBundle) {
        foreach (var assetBundleName in assetBundle.GetAllAssetNames()) {
            if (assetBundleName.Contains(MANIFEST)) {
                var manifest = assetBundle.LoadAsset<AssetBundleManifest>(assetBundleName);
                return manifest;
            }
        }

        return null;
    }
}
