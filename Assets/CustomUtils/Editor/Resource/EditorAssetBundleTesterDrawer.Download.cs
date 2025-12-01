using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

public partial class EditorAssetBundleTesterDrawer {
    
    private EditorCoroutine _ensureRepaintCoroutine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawDownload() {
        GUILayout.Label($"{nameof(AssetBundleManifest)} 다운로드 테스트", Constants.Draw.AREA_TITLE_STYLE);
        using (new GUILayout.VerticalScope("box")) {
            using (new GUILayout.HorizontalScope()) {
                EditorCommon.DrawLabelToggle(ref config.isActiveCustomManifestPath, "임의 서버 경로 입력 활성화", 150f);
                EditorCommon.DrawLabelToggle(ref config.isActiveLocalSave, "로컬 저장 활성화", 150f);
                GUILayout.FlexibleSpace();
            }

            if (config.isActiveCustomManifestPath) {
                EditorCommon.DrawLabelTextField("임의 서버 경로", ref config.customManifestPath);
            }

            EditorCommon.DrawLabelTextField("AssetBundleManifest 다운로드 경로", config.GetManifestPath(), 210f);

            GUILayout.Space(5f);

            EditorCommon.DrawFileOpenSelector(ref config.localManifestPath, "로컬 Manifest 파일", "선택", onSelect: () => {
                if (IOUtil.TryReadBytes(config.localManifestPath, out var bytes)) {
                    AssetBundle.UnloadAllAssetBundles(false);
                    if (AssetBundleUtil.TryLoadFromMemoryOrDecrypt(out var assetBundle, bytes, _plainEncryptKey) && assetBundle.TryFindManifest(out var manifest)) {
                        _bindManifestInfo = new AssetBundleManifestInfo(manifest, Path.GetFileName(config.localManifestPath));
                        Repaint();
                    }
                }
            });

            if (GUILayout.Button("Manifest 다운로드")) {
                AssetBundle.UnloadAllAssetBundles(false);
                DownloadAssetBundleManifest(config.GetManifestPath(), 0, callback: (result, manifest) => {
                    if (result == UnityWebRequest.Result.Success && manifest != null) {
                        _bindManifestInfo = new AssetBundleManifestInfo(manifest, Path.GetFileName(config.GetManifestPath()));
                        Repaint();
                    }
                });
            }

            EditorCommon.DrawSeparator();

            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawLabelTextField("연결 상태", _bindManifestInfo?.IsValid() ?? false ? "연결".GetColorString(Color.green) : "미연결".GetColorString(Color.red));
                if (_bindManifestInfo?.IsValid() ?? false) {
                    EditorCommon.DrawLabelTextField("로드 시간", _bindManifestInfo.loadTime.ToString(CultureInfo.CurrentCulture));

                    _manifestScrollViewPosition = EditorGUILayout.BeginScrollView(_manifestScrollViewPosition, false, false, GUILayout.Height(100f));
                    using (new GUILayout.VerticalScope()) {
                        foreach (var name in _bindManifestInfo.manifest.GetAllAssetBundles()) {
                            using (new GUILayout.HorizontalScope()) {
                                EditorGUILayout.LabelField(name);
                                if (GUILayout.Button("다운로드")) {
                                    AssetBundle.UnloadAllAssetBundles(false);
                                    var path = $"{config.selectBuildTarget}/{name}";
                                    if (config.isActiveCaching) {
                                        DownloadAssetBundle(path, _bindManifestInfo.manifest.GetAssetBundleHash(name), callback: OnAssetBundleDownloadComplete);
                                    } else {
                                        DownloadAssetBundle(path, callback: OnAssetBundleDownloadComplete);
                                    }
                                }

                                if (GUILayout.Button("다운로드(암호화)")) {
                                    AssetBundle.UnloadAllAssetBundles(false);
                                    var path = $"{config.selectBuildTarget}/{name}";
                                    (uint crc, Hash128? hash) info = (0, null);
                                    _bindChecksumInfo?.TryGetChecksum(name, out info);
                                    DownloadAssetBundle(path, info.crc, OnAssetBundleDownloadComplete);
                                }
                            }
                        }
                    }

                    EditorGUILayout.EndScrollView();

                    if (_downloadQueue.IsEmpty == false) {
                        EditorCommon.DrawSeparator();

                        if (GUILayout.Button("정리")) {
                            while (_downloadQueue.TryDequeue(out var download)) {
                                download?.Dispose();
                            }
                        }

                        _downloadQueueScrollViewPosition = EditorGUILayout.BeginScrollView(_downloadQueueScrollViewPosition, false, false, GUILayout.MinHeight(100f));
                        var count = _downloadQueue.Count - 1;
                        for (var i = 0; i <= count; i++) {
                            if (_downloadQueue.TryDequeue(out var download)) {
                                if (download.IsValid() == false) {
                                    download.Dispose();
                                }

                                using (new EditorGUILayout.HorizontalScope()) {
                                    EditorGUILayout.LabelField(download.GetName());
                                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true)), download.GetProgress(), download.GetDisplayProgress());
                                }

                                if (download.IsDone() == false) {
                                    _downloadQueue.Enqueue(download);
                                }
                            }
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }
            }

            EditorCommon.DrawSeparator();

            if (GUILayout.Button("Manifest AssetBundle 다운로드 테스트")) {
                AssetBundle.UnloadAllAssetBundles(false);
                if (_bindManifestInfo != null && _bindManifestInfo.IsValid()) {
                    foreach (var assetBundleName in _bindManifestInfo.manifest.GetAllAssetBundles()) {
                        var path = $"{config.selectBuildTarget}/{assetBundleName}";
                        if (config.isActiveCaching) {
                            if (_bindChecksumInfo != null && _bindChecksumInfo.TryGetChecksum(assetBundleName, out var info)) {
                                DownloadAssetBundle(path, info.hash, info.crc, OnAssetBundleDownloadComplete);
                            } else {
                                DownloadAssetBundle(path, _bindManifestInfo.manifest.GetAssetBundleHash(assetBundleName), callback: OnAssetBundleDownloadComplete);
                            }
                        } else {
                            DownloadAssetBundle(path, callback: OnAssetBundleDownloadComplete);
                        }
                    }
                }
            }

            if (GUILayout.Button("Manifest AssetBundle 다운로드 테스트(암호화)")) {
                AssetBundle.UnloadAllAssetBundles(false);
                if (_bindManifestInfo != null && _bindManifestInfo.IsValid()) {
                    foreach (var assetBundleName in _bindManifestInfo.manifest.GetAllAssetBundles()) {
                        (uint crc, Hash128? hash) info = (0, null);
                        _bindChecksumInfo?.TryGetChecksum(assetBundleName, out info);
                        DownloadAssetBundle($"{config.selectBuildTarget}/{assetBundleName}", info.crc, OnAssetBundleDownloadComplete);
                    }
                }
            }
        }
    }
    
    private void DownloadJsonFile<T>(string serverPath, Action<Result, T> callback = null) where T : class {
        _downloadService.Download(new JsonDownloadHandler<T>(Path.Combine(config.url, serverPath), _plainEncryptKey), (result, handler) => {
            using (handler) {
                if (result == Result.Success) {
                    callback?.Invoke(result, handler.GetContent());
                }
            }
        });
    }

    private void DownloadAssetBundleManifest(string name, uint crc = 0, Action<Result, AssetBundleManifest> callback = null) => StartCoroutine(EnsureUniqueDownload(name, DownloadAssetBundleManifestAsync(name, crc, callback)));
    private void DownloadAssetBundle(string name, Hash128? hash, uint crc = 0, Action<Result, AssetBundle> callback = null) => StartCoroutine(EnsureUniqueDownload(name, DownloadAssetBundleAsync(name, hash, crc, callback)));
    private void DownloadAssetBundle(string name, uint crc = 0, Action<Result, AssetBundle> callback = null) => StartCoroutine(EnsureUniqueDownload(name, DownloadAssetBundleAsync(name, crc, callback)));

    private IEnumerator EnsureUniqueDownload(string key, IEnumerator enumerator) {
        if (_ensureUniqueSet.Contains(key) == false) {
            _ensureUniqueSet.Add(key);
            yield return StartCoroutine(enumerator);
            _ensureUniqueSet.Remove(key);
        } else {
            Logger.TraceLog($"Already download || {key}", Color.yellow);
        }
    }

    private IEnumerator DownloadAssetBundleManifestAsync(string name, uint crc = 0, Action<Result, AssetBundleManifest> callback = null) {
        var path = Path.Combine(config.url, name);
        using (var handler = new AssetBundleManifestDownloadHandler(path, crc, _plainEncryptKey)) {
            var headerOperation = _downloadService.DownloadHeader(path);
            var downloadOperation = _downloadService.Download(handler);
            
            yield return headerOperation;
            _downloadQueue.Enqueue(new AssetBundleDownloadOperation(headerOperation.GetContentLength(), name, downloadOperation));
            yield return StartCoroutine(WaitAsyncOperationComplete(downloadOperation));

            if (handler.webRequest.result != Result.Success) {
                callback?.Invoke(handler.webRequest.result, null);
                yield break;
            }

            if (handler.TryGetContent(out var manifest)) {
                if (config.isActiveLocalSave && IOUtil.TryWriteBytes(config.GetManifestDownloadPath(), handler.data, out _) == false) {
                    Logger.TraceError($"Failed to save the {nameof(AssetBundleManifest)} to the path {config.GetManifestDownloadPath()}");
                }
                
                callback?.Invoke(handler.webRequest.result, manifest);
            } else {
                Logger.TraceError("Failed to create AssetBundleManifest");
                callback?.Invoke(Result.DataProcessingError, null);
            }
        }
    }

    private IEnumerator DownloadAssetBundleAsync(string name, Hash128? hash, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var path = Path.Combine(config.url, name);
        var headerOperation = _downloadService.DownloadHeader(path);
        var downloadOperation = _downloadService.DownloadAssetBundle(path, hash, crc, callback);
        
        yield return headerOperation;
        _downloadQueue.Enqueue(new AssetBundleDownloadOperation(headerOperation.GetContentLength(), name, downloadOperation));
        yield return StartCoroutine(WaitAsyncOperationComplete(downloadOperation));
    }
    
    private IEnumerator DownloadAssetBundleAsync(string name, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var path = Path.Combine(config.url, name);
        using (var handler = new AssetBundleDownloadHandler(path, crc, _plainEncryptKey)) {
            var headerOperation = _downloadService.DownloadHeader(path);
            var downloadOperation = _downloadService.Download(handler);

            yield return headerOperation;
            _downloadQueue.Enqueue(new AssetBundleDownloadOperation(headerOperation.GetContentLength(), name, downloadOperation));
            yield return StartCoroutine(WaitAsyncOperationComplete(downloadOperation));
            
            var result = handler.webRequest.result;
            if (result != Result.Success) {
                callback?.Invoke(handler.webRequest.result, null);
                yield break;
            }

            var assetBundleOperation = handler.GetContentFromMemoryAsync();
            _downloadQueue.Enqueue(new AssetBundleLoadOperation(name, assetBundleOperation));
            yield return StartCoroutine(WaitAsyncOperationComplete(assetBundleOperation));
            
            if (assetBundleOperation.assetBundle != null) {
                if (config.isActiveLocalSave) {
                    var savePath = $"{config.downloadDirectory}/{assetBundleOperation.assetBundle.name}";
                    if (IOUtil.TryWriteBytes(savePath, handler.data, out _) == false) {
                        Logger.TraceError($"Failed to save the {nameof(AssetBundle)} to the path {savePath}");
                    }
                }

                callback?.Invoke(result, assetBundleOperation.assetBundle);
            } else {
                callback?.Invoke(result, null);
            }
        }
    }

    private IEnumerator WaitAsyncOperationComplete(AsyncOperation operation) {
        if (_ensureRepaintCoroutine != null) {
            while (operation.isDone == false) {
                yield return null;
                if (_ensureRepaintCoroutine == null) {
                    yield return EnsureRepaintCoroutine(RepaintProgressBar(operation));
                }
            }
        } else {
            yield return EnsureRepaintCoroutine(RepaintProgressBar(operation));
        }
    }

    private IEnumerator EnsureRepaintCoroutine(IEnumerator enumerator) {
        _ensureRepaintCoroutine = StartCoroutine(enumerator);
        yield return _ensureRepaintCoroutine;
        _ensureRepaintCoroutine = null;
    }

    private IEnumerator RepaintProgressBar(UnityEngine.AsyncOperation operation, float interval = 0.1f) {
        while (operation.isDone == false) {
            yield return new EditorWaitForSeconds(interval);
            Repaint();
        }
    }
    
    private void OnAssetBundleDownloadComplete(Result result, AssetBundle assetBundle) {
        switch (result) {
            case Result.Success:
                Logger.TraceLog($"{assetBundle.name} Download Success", Color.green);
                break;
            case Result.DataProcessingError:
                Logger.TraceError("The asset bundle may be encrypted. Please check the asset bundle build options again.");
                break;
        }
    }
    
    #region [Progress]
    
    private abstract record AsyncProgressOperation : IDisposable {

        private readonly string _target;

        public AsyncProgressOperation(string target) => _target = target;
        
        ~AsyncProgressOperation() => Dispose();
        
        public abstract void Dispose();
        public abstract float GetProgress();
        public abstract string GetDisplayProgress();
        public abstract bool IsDone();
        public abstract bool IsValid();
        
        public virtual string GetName() => _target;
    }
    
    private record AssetBundleDownloadOperation : AsyncProgressOperation {

        private readonly UnityWebRequest _request;
        private readonly ulong _totalBytes;

        public AssetBundleDownloadOperation(ulong totalBytes, string target, UnityWebRequestAsyncOperation operation) : this(target, operation) => _totalBytes = totalBytes;
        public AssetBundleDownloadOperation(string target, UnityWebRequestAsyncOperation operation) : base(target) => _request = operation.webRequest;

        public override void Dispose() => _request?.Dispose();
        public override float GetProgress() => _request?.downloadProgress ?? 1f;
        public override string GetDisplayProgress() => _totalBytes > 0 ? $"Download ({GetProgress():P})  ({_request?.downloadedBytes ?? 0} / {_totalBytes})" : $"Download ({GetProgress():P})  ({_request?.downloadedBytes ?? 0})";
        public override bool IsDone() => _request?.isDone ?? true;
        public override bool IsValid() => _request != null;
    }

    private record AssetBundleLoadOperation : AsyncProgressOperation {

        private readonly AssetBundleCreateRequest _request;

        public AssetBundleLoadOperation(string target, AssetBundleCreateRequest request) : base(target) => _request = request;

        public override void Dispose() { }
        public override float GetProgress() => _request?.progress ?? 1f;
        public override string GetDisplayProgress() => $"Load from Memory ({GetProgress():P})";
        public override bool IsDone() => _request?.isDone ?? true;
        public override bool IsValid() => _request != null;
    }

    #endregion
}