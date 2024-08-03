using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class JsonDownloadHandler<TReturn> : DownloadHandlerModule<TReturn> where TReturn : class{

    private string _encryptKey;
    private string _text;
        
    public JsonDownloadHandler(string url, string encryptKey) : base(url) => _encryptKey = encryptKey;

    protected override string GetText() {
        if (string.IsNullOrEmpty(_text)) {
            _text = GetNativeData().ToArray().GetString();
        }
        
        return _text;
    }

    public override bool TryGetContent(out TReturn content) {
        content = GetContent();
        return content != null;
    }

    public override TReturn GetContent() {
        TReturn content = null;
        try {
            content = JsonConvert.DeserializeObject<TReturn>(GetText());
        } catch (JsonReaderException) {
            if (EncryptUtil.TryDecryptAES(out var json, text, _encryptKey)) {
                content = JsonConvert.DeserializeObject<TReturn>(json);
            }
        }

        return content;
    }
}