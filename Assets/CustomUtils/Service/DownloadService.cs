using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

public class DownloadService : IService {
    
    void IService.Start() { }
    void IService.Stop() { }

    public UnityWebRequestAsyncOperation DownloadHeader(string url, Action<Result, UnityWebRequest> callback = null) {
        var operation = UnityWebRequest.Head(url).SendWebRequest();
        operation.completed += _ => {
            var request = operation.webRequest;
            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
            }
            
            callback?.Invoke(request.result, request);
        };

        return operation;
    }

    [TestRequired]
    public async Task<Header> DownloadHeaderAsync(string url) {
        var operation = UnityWebRequest.Head(url).SendWebRequest();
        await operation;
        return operation.webRequest.result != Result.Success ? throw new InvalidOperationException($"{operation.webRequest.result} || {operation.webRequest.error}") : new Header(operation);
    }

    public UnityWebRequestAsyncOperation Download(string url, Action<Result, byte[]> callback) => Download<DownloadHandler>(CreateGet(url), (result, handler) => callback?.Invoke(result, result != Result.Success ? Array.Empty<byte>() : handler.data));
    
    public UnityWebRequestAsyncOperation Download<THandler>(THandler handler, Action<Result, THandler> callback) where THandler : DownloadHandler, IDownloadHandlerModule => Download(handler.CreateWebRequest(), callback);
    public UnityWebRequestAsyncOperation Download<THandler, TReturn>(THandler handler, Action<Result, TReturn> callback) where THandler : DownloadHandlerModule<TReturn> => Download<THandler>(handler.CreateWebRequest(), (result, requestHandler) => callback?.Invoke(result, requestHandler.GetContent()));
    public UnityWebRequestAsyncOperation Download<THandler>(string url, THandler handler, Action<Result, THandler> callback) where THandler : DownloadHandler => Download(CreateGet(url, handler), callback);

    public UnityWebRequestAsyncOperation Download<THandler>(UnityWebRequest request, Action<Result, THandler> callback) where THandler : DownloadHandler {
        var operation = request.SendWebRequest();
        try {
            operation.completed += _ => {
                if (request.responseCode != (long) HttpStatusCode.OK) {
                    Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
                    if (request.downloadHandler is THandler downloadHandler) {
                        callback?.Invoke(request.result, downloadHandler);
                    }
                    
                    return;
                }

                if (request.result != Result.Success) {
                    callback?.Invoke(request.result, null);
                } else if (request.downloadHandler is not THandler downloadHandler) {
                    callback?.Invoke(Result.DataProcessingError, null);
                } else {
                    callback?.Invoke(request.result, downloadHandler);
                }
            };
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return operation;
    }
    
    public UnityWebRequestAsyncOperation Download<THandler>(THandler handler) where THandler : DownloadHandler, IDownloadHandlerModule => handler.CreateWebRequest().SendWebRequest();
    public UnityWebRequestAsyncOperation Download<THandler>(string url, THandler handler) where THandler : DownloadHandler => CreateGet(url, handler).SendWebRequest();
    
    public async Task<THandler> DownloadAsync<THandler>(THandler handler) where THandler : DownloadHandler, IDownloadHandlerModule => await DownloadAsync<THandler>(handler.CreateWebRequest());
    public async Task<THandler> DownloadAsync<THandler, TReturn>(THandler handler) where THandler : DownloadHandlerModule<TReturn> => await DownloadAsync<THandler>(handler.CreateWebRequest());
    public async Task<THandler> DownloadAsync<THandler>(string url, THandler handler) where THandler : DownloadHandler => await DownloadAsync<THandler>(new UnityWebRequest(url, "GET", handler, null));
    
    public async Task<THandler> DownloadAsync<THandler>(UnityWebRequest request) where THandler : DownloadHandler {
        var operation = request.SendWebRequest();
        await operation;
        return operation.webRequest.result != Result.Success ? throw new InvalidOperationException($"{operation.webRequest.result} || {operation.webRequest.error}") : operation.webRequest.downloadHandler as THandler;
    }

    #region [Texture]
    
    public void DownloadTexture(string url, Action<Result, Texture> callback) => Download(url, new DownloadHandlerTexture(false), (result, handler) => callback?.Invoke(result, handler.texture));

    public async Task<Texture2D> DownloadTextureAsync(string url) {
        var handler = new DownloadHandlerTexture(false);
        var operation = Download(url, handler);
        await operation;
        return operation.webRequest.result == Result.Success ? handler.texture : throw new InvalidOperationException($"{operation.webRequest.result} || {operation.webRequest.error}");
    }
    
    #endregion
    
    #region [AssetBundle]
    
    public UnityWebRequestAsyncOperation DownloadAssetBundle(string url, Hash128? hash, uint crc = 0, Action<Result, AssetBundle> callback = null) => DownloadAssetBundle(url, CreateDownloadHandlerAssetBundle(url, hash, crc), (result, assetBundle) => callback?.Invoke(result, assetBundle)); 
    
    private UnityWebRequestAsyncOperation DownloadAssetBundle(string url, DownloadHandler handler, Action<Result, AssetBundle> callback = null) {
        var request = CreateGet(url, handler);
        var operation = request.SendWebRequest();
        operation.completed += _ => {
            if (request.responseCode == (long) HttpStatusCode.OK) {
                if (request.result != Result.Success) {
                    Logger.TraceError(request.error);
                    callback?.Invoke(request.result, null);
                    return;
                }
            }
#if UNITY_EDITOR
            else if (request.responseCode == 0) {
                Logger.TraceLog("Resource was obtained through the caching system.", Color.magenta);
            } 
#endif
            else {
                Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
                callback?.Invoke(request.result, null);
                return;
            }

            try {
                var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                if (assetBundle != null) {
                    callback?.Invoke(request.result, assetBundle);
                } else {
                    Logger.TraceError($"Download succeeded, but failed to acquire the {nameof(AssetBundle)} due to an unknown reason");
                }
            } catch (Exception ex) {
                Logger.TraceLog(ex, Color.red);
            }
        };
        
        return operation;
    }
    
    
    public Task<DownloadHandlerAssetBundle> DownloadAssetBundleAsync(string url, Hash128? hash, uint crc = 0) => DownloadAssetBundleAsync(url, CreateDownloadHandlerAssetBundle(url, hash, crc)); 
    
    public Task<DownloadHandlerAssetBundle> DownloadAssetBundleAsync(string url, DownloadHandlerAssetBundle handler) {
        var completionSource = new TaskCompletionSource<DownloadHandlerAssetBundle>();
        var request = CreateGet(url, handler);
        request.SendWebRequest().completed += _ => completionSource.SetResult(handler);
        return completionSource.Task;
    }
    
    
    #endregion
    
    public UnityWebRequest CreateGet(string url) => UnityWebRequest.Get(url);
    public UnityWebRequest CreateGet(string url, DownloadHandler handler) => new(url, HttpMethod.Get.Method, handler, null);

    public DownloadHandlerAssetBundle CreateDownloadHandlerAssetBundle(string url, Hash128? hash, uint crc = 0) => hash.HasValue ? new DownloadHandlerAssetBundle(url, hash.Value, crc) : new DownloadHandlerAssetBundle(url, crc);
}

public struct Header {

    public ulong contentLength;
    public string contentType;

    public Header(UnityWebRequestAsyncOperation operation) {
        contentLength = operation.GetContentLength();
        contentType = operation.webRequest.GetResponseHeader(HttpResponseHeader.ContentType.GetName());
    }
}