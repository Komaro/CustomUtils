using System.Net.Http;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleManifestDownloadHandler : DownloadHandlerModule<AssetBundleManifest> {

    private readonly uint _crc;
    private readonly string _encryptKey;

    public AssetBundleManifestDownloadHandler(string url, uint crc = 0) : this(url, crc, string.Empty) { }
    
    public AssetBundleManifestDownloadHandler(string url, uint crc, string encryptKey) : base(url) {
        _crc = crc;
        _encryptKey = encryptKey;
    }

    public override bool TryGetContent(out AssetBundleManifest content) => (content = GetContent()) != null;

    public override AssetBundleManifest GetContent() {
        if (AssetBundleUtil.TryLoadFromMemoryOrDecrypt(out var assetBundle, GetData(), _encryptKey, _crc) && assetBundle.TryFindManifest(out var manifest)) {
            return manifest;
        }
        
        return null;
    }
}