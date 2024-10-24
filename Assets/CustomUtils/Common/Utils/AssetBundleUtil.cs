using UnityEngine;

public static class AssetBundleUtil {

    public static bool TryLoadFromFile(out AssetBundle assetBundle, string path, uint crc = 0) => (assetBundle = AssetBundle.LoadFromFile(path, crc)) != null;

    public static bool TryLoadManifestFromFile(out AssetBundleManifest manifest, string path, uint crc = 0) {
        if (TryLoadFromFile(out var assetBundle, path, crc) && assetBundle.TryFindManifest(out manifest)) {
            return true;
        }

        manifest = null;
        return false;
    }

    public static bool TryLoadFromMemoryOrDecrypt(out AssetBundle assetBundle, byte[] bytes, string key, uint crc = 0, ENCRYPT_TYPE type = default) => (assetBundle = LoadFromMemoryOrDecrypt(bytes, key, crc, type)) != null;
    public static AssetBundle LoadFromMemoryOrDecrypt(byte[] bytes, string key, uint crc = 0, ENCRYPT_TYPE type = default) => AssetBundle.LoadFromMemory(bytes, crc) ?? AssetBundle.LoadFromMemory(EncryptUtil.Decrypt(bytes, key, type), crc);
}