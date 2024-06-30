using System;
using UnityEngine;

public static class AssetBundleUtil {
    
    public static bool TryLoadFromMemoryOrDecrypt(byte[] bytes, string key, out AssetBundle assetBundle, uint crc = 0, ENCRYPT_TYPE type = default) {
        assetBundle = LoadFromMemoryOrDecrypt(bytes, key, crc, type);
        return assetBundle != null;
    }
    
    public static AssetBundle LoadFromMemoryOrDecrypt(byte[] bytes, string key, uint crc = 0, ENCRYPT_TYPE type = default) => AssetBundle.LoadFromMemory(bytes, crc) ?? AssetBundle.LoadFromMemory(EncryptUtil.Decrypt(bytes, key, type), crc);
}