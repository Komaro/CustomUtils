using System;
using System.Net.Http;
using Unity.Collections;
using UnityEngine.Networking;

public interface IDownloadHandlerModule {

    public UnityWebRequest CreateWebRequest();
    public UnityWebRequest GetWebRequest();
}

public abstract class DownloadHandlerModule<TReturn> : DownloadHandlerScript, IDownloadHandlerModule {
    
    public readonly string url;

    protected NativeArray<byte> buffer;
    private int _index;
    
    public UnityWebRequest webRequest;

    public DownloadHandlerModule(string url) => this.url = url;

    ~DownloadHandlerModule() {
        Dispose();
        GC.SuppressFinalize(this);
    }

    public override void Dispose() {
        if (buffer.IsCreated) {
            buffer.Dispose();
        }
        
        base.Dispose();
    }

    public virtual UnityWebRequest CreateWebRequest() => webRequest = new UnityWebRequest(url, HttpMethod.Get.Method, this, null);

    public UnityWebRequest GetWebRequest() => webRequest;

    protected override void ReceiveContentLengthHeader(ulong contentLength) {
        if (buffer.IsCreated) {
            buffer.Dispose();
        }

        buffer = new NativeArray<byte>((int) contentLength, Allocator.Persistent);
        base.ReceiveContentLengthHeader(contentLength);
    }

    protected override bool ReceiveData(byte[] data, int dataLength) {
        if (_index + dataLength > buffer.Length) {
            return false;
        }
        
        NativeArray<byte>.Copy(data, 0, buffer, _index, dataLength);
        _index += dataLength;
        return true;
    }

    protected override NativeArray<byte> GetNativeData() => buffer.IsCreated ? buffer : new NativeArray<byte>();
    protected override float GetProgress() => (float)_index / buffer.Length;

    public abstract bool TryGetContent(out TReturn content);
    public abstract TReturn GetContent();
}