using UnityEngine;

public static class AssetBundleExtension {

    private const string MANIFEST = "manifest";
    
    public static bool TryFindManifest(this AssetBundle assetBundle, out AssetBundleManifest manifest) => (manifest = assetBundle.FineManifest()) != null;

    public static AssetBundleManifest FineManifest(this AssetBundle assetBundle) {
        foreach (var name in assetBundle.GetAllAssetNames()) {
            if (name.Contains(MANIFEST)) {
                var manifest = assetBundle.LoadAsset<AssetBundleManifest>(name);
                return manifest;
            }
        }

        return null;
    }
}
