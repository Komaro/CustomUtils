using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

public class DownloadService : IService {

    private UnityMainThreadDispatcherService _threadDispatcherService;

    void IService.Init() => _threadDispatcherService = Service.GetService<UnityMainThreadDispatcherService>();
    void IService.Start() { }
    void IService.Stop() { }

    public void DownloadHeader(string url, Action<Result, ulong> callback) {
        var request = UnityWebRequest.Head(url);
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
                return;
            }
            
            Logger.TraceLog(request.GetResponseHeaders().ToStringCollection(x => $"{x.Key} | {x.Value}", '\n'));
            if (ulong.TryParse(request.GetResponseHeader(HttpResponseHeader.ContentLength.GetName()), out var length)) {
                callback?.Invoke(request.result, length);
            }
        };
    }

    public UnityWebRequestAsyncOperation DownloadHeader(string url, [CanBeNull] Action<Result, UnityWebRequest> callback = null) {
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

    public UnityWebRequestAsyncOperation Download(string url, Action<Result, byte[]> callback) => Download<DownloadHandler>(CreateGet(url), (result, handler) => callback?.Invoke(result, result != Result.Success ? Array.Empty<byte>() : handler.data));
    
    public UnityWebRequestAsyncOperation Download<THandler>(THandler handler, Action<Result, THandler> callback) where THandler : DownloadHandler, IDownloadHandlerModule => Download(handler.CreateWebRequest(), callback);
    
    public UnityWebRequestAsyncOperation Download<THandler, TReturn>(THandler handler, Action<Result, TReturn> callback) where THandler : DownloadHandlerModule<TReturn> => Download<THandler>(handler.CreateWebRequest(), (result, requestHandler) => {
        if (result == Result.Success) {
            callback?.Invoke(result, requestHandler.GetContent());
        }
    });

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
    
    public UnityWebRequestAsyncOperation Download<THandler>(THandler handler) where THandler : DownloadHandler, IDownloadHandlerModule => Download<THandler>(handler.CreateWebRequest());
    public UnityWebRequestAsyncOperation Download<THandler>(string url, THandler handler) where THandler : DownloadHandler => Download<THandler>(CreateGet(url, handler));
    public UnityWebRequestAsyncOperation Download<THandler>(UnityWebRequest request) where THandler : DownloadHandler => request.SendWebRequest();


    // TODO. 사용 검토
    public void AsyncDownload(string url, Action<Result, byte[]> callback, Action<float> onProgress) => AsyncDownload<DownloadHandler>(CreateGet(url), (result, handler) => callback?.Invoke(result, result != Result.Success ? Array.Empty<byte>() : handler.data), onProgress);
    public void AsyncDownload<THandler>(THandler handler, Action<Result, THandler> callback, Action<float> onProgress) where THandler : DownloadHandler, IDownloadHandlerModule => AsyncDownload(handler.CreateWebRequest(), callback, onProgress);
    
    public void AsyncDownload<THandler, TReturn>(THandler handler, Action<Result, TReturn> callback, Action<float> onProgress) where THandler : DownloadHandlerModule<TReturn> => AsyncDownload<THandler>(handler.CreateWebRequest(), (result, requestHandler) => {
        if (result == Result.Success) {
            callback?.Invoke(result, requestHandler.GetContent());
        }
    }, onProgress);

    public void AsyncDownload<THandler>(string url, THandler handler, Action<Result, THandler> callback, Action<float> onProgress) where THandler : DownloadHandler => AsyncDownload(new UnityWebRequest(url, "GET", handler, null), callback, onProgress);
    
    public void AsyncDownload<THandler>(UnityWebRequest request, Action<Result, THandler> callback, Action<float> onProgress) where THandler : DownloadHandler {
        if (_threadDispatcherService == null) {
            Logger.TraceError($"{nameof(_threadDispatcherService)} is null");
            return;
        }
        
        _threadDispatcherService.Enqueue(DownloadCoroutine(request, callback, onProgress));
    }

    private IEnumerator DownloadCoroutine<THandler>(UnityWebRequest request, Action<Result, THandler> callback, Action<float> onProgress) where THandler : DownloadHandler {
        using (request) {
            var async = request.SendWebRequest();
            while (async.isDone == false) {
                onProgress?.Invoke(async.progress);
                yield return null;
            }

            if (request.result != Result.Success) {
                callback?.Invoke(request.result, null);
            } else if (request.downloadHandler is not THandler downloadHandler) {
                callback?.Invoke(Result.DataProcessingError, null);
            } else {
                callback?.Invoke(request.result, downloadHandler);
            }
        }
    }

    #region [Texture]
    
    public void DownloadTexture(string url, Action<Result, Texture> callback) => Download(url, new DownloadHandlerTexture(false), (result, handler) => callback?.Invoke(result, handler.texture));
    
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
                Logger.TraceLog($"Resource was obtained through the caching system.", Color.magenta);
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
    
    #endregion
    
    public UnityWebRequest CreateGet(string url) => UnityWebRequest.Get(url);
    public UnityWebRequest CreateGet(string url, DownloadHandler handler) => new(url, HttpMethod.Get.Method, handler, null);

    public DownloadHandler CreateDownloadHandlerAssetBundle(string url, Hash128? hash, uint crc = 0) => hash.HasValue ? new DownloadHandlerAssetBundle(url, hash.Value, crc) : new DownloadHandlerAssetBundle(url, crc);
}