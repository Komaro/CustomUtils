using System.IO;
using UnityEngine;

// DownloadHandlerAssetBundle에 비해 AssetBundle 접근 속도가 떨어짐. 암호화가 필요한 경우가 아니면 사용을 지양
public sealed class AssetBundleDownloadHandler : DownloadHandlerModule<AssetBundle> {

    public readonly uint crc;
    public readonly string encryptKey;

    public AssetBundleDownloadHandler(string url, uint crc, string encryptKey) : base(url) {
        this.crc = crc;
        this.encryptKey = encryptKey;
    }
    
    public override bool TryGetContent(out AssetBundle content) => (content = GetContent()) != null;
    public override AssetBundle GetContent() => AssetBundleUtil.TryLoadFromMemoryOrDecrypt(out var assetBundle, GetData(), encryptKey, crc) ? assetBundle : null;

    public AssetBundleCreateRequest GetContentFromMemoryAsync() => AssetBundle.LoadFromMemoryAsync(buffer.ToArray(), crc);

    public AssetBundleCreateRequest GetContentFromStreamAsync() {
        var stream = new MemoryStream(buffer.ToArray());
        var request = AssetBundle.LoadFromStreamAsync(stream, crc);
        request.completed += _ => stream.Dispose();
        return request;
    }
}