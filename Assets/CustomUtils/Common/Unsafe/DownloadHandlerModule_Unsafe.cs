using System;
using System.Net.Http;
using Unity.Collections;
using UnityEngine.Networking;

// TODO. ReceiveContentLengthHeader 받지 못하는 경우 buffer가 생성되지 않아 에러가 발생하는 케이스 확인하여 해당 이슈를 해결 및 최적화를 동시에 처리하기 위한 Unsafe 구조 구현 테스트
public abstract class DownloadHandlerModule_Unsafe<TReturn> : DownloadHandlerScript, IDownloadHandlerModule {
    
    public readonly string url;

    protected int contentLength;
    protected NativeList<byte> listBuffer = new(128, Allocator.Persistent);
    
    public UnityWebRequest webRequest;

    protected bool isDisposed;

    public DownloadHandlerModule_Unsafe(string url) => this.url = url;

    ~DownloadHandlerModule_Unsafe() {
        if (isDisposed == false) {
            Dispose();
        }
    }

    public override void Dispose() {
        if (isDisposed == false) {
            if (listBuffer.IsCreated) {
                listBuffer.Dispose();
            }
            
            base.Dispose();
            GC.SuppressFinalize(this);

            isDisposed = true;
        }
    }

    public virtual UnityWebRequest CreateWebRequest() => webRequest = new UnityWebRequest(url, HttpMethod.Get.Method, this, null);

    public UnityWebRequest GetWebRequest() => webRequest;

    protected override void ReceiveContentLengthHeader(ulong contentLength) {
        this.contentLength = (int) contentLength;
        if (listBuffer.IsCreated && listBuffer.Length > this.contentLength) {
            listBuffer.Resize(this.contentLength, NativeArrayOptions.ClearMemory);
        }
        
        base.ReceiveContentLengthHeader(contentLength);
    }

    protected override bool ReceiveData(byte[] data, int dataLength) {
        if (data.IsEmpty() || dataLength <= 0) {
            return false;
        }
        
        unsafe {
            fixed (byte* ptr = data) {
                var leftCapacity = listBuffer.Capacity - listBuffer.Length;
                if (leftCapacity >= dataLength) {
                    listBuffer.AddRangeNoResize(ptr, dataLength);
                } else {
                    listBuffer.AddRange(ptr, dataLength);
                }
            }
        }
        
        return true;
    }

    protected override NativeArray<byte> GetNativeData() => listBuffer.IsCreated ? listBuffer.ToArray(Allocator.Temp) : new NativeArray<byte>();
    protected override float GetProgress() => contentLength <= 0 ? 0f : (float)listBuffer.Length / contentLength;

    public abstract bool TryGetContent(out TReturn content);
    public abstract TReturn GetContent();
}