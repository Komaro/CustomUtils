using UnityEngine;
using UnityEngine.Networking;

public sealed class AssetBundleDownloadHandler : DownloadHandlerScript {

    public readonly uint crc;
    public readonly string encryptKey;

    private byte[] _bytes;

    public AssetBundleDownloadHandler(uint crc, string encryptKey) {
        this.crc = crc;
        this.encryptKey = encryptKey;
    }
    
    protected override byte[] GetData() => _bytes;

    protected override bool ReceiveData(byte[] data, int dataLength) {
        _bytes = data;
        return base.ReceiveData(data, dataLength);
    }

    public static UnityWebRequest CreateUnityWebRequest(string path, uint crc = 0, string encryptKey = "") => new(path, "GET", new AssetBundleDownloadHandler(crc, encryptKey), null);
    
    public static bool TryGetContent(UnityWebRequest request, out AssetBundle assetBundle) {
        assetBundle = GetContent(request);
        return assetBundle != null;
    }
    
    public static AssetBundle GetContent(UnityWebRequest request) {
        var handler = GetCheckedDownloader<AssetBundleDownloadHandler>(request);
        return handler != null && AssetBundleUtil.TryLoadFromMemoryOrDecrypt(handler.data, handler.encryptKey, out var assetBundle, handler.crc) ? assetBundle : null;
    }
}