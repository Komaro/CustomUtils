using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

public sealed class JsonDownloadHandler : DownloadHandlerScript {

    private byte[] _bytes;
    private string _text;

    protected override byte[] GetData() => _bytes;
    protected override string GetText() => _text;

    protected override bool ReceiveData(byte[] data, int dataLength) {
        _bytes = data;
        _text = data.GetString();
        return base.ReceiveData(data, dataLength);
    }

    public static UnityWebRequest CreateUnityWebRequest(string path) {
        return new(path, "GET", new JsonDownloadHandler(), null);
    }

    public static bool TryGetContent<T>(UnityWebRequest request, out T content) {
        content = GetContent<T>(request);
        return content != null;
    }

    public static T GetContent<T>(UnityWebRequest request) {
        var handler = GetCheckedDownloader<JsonDownloadHandler>(request);
        if (handler != null) {
            var content = JsonConvert.DeserializeObject<T>(handler.text);
        
            return content;
        }

        return default;
    }
}