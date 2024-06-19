using System;
using System.IO;
using System.Net;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Test, typeof(AssetBundleProvider))]
public class EditorAssetBundleTesterDrawer : EditorResourceDrawerAutoConfig<AssetBundleTesterConfig, AssetBundleTesterConfig.NullConfig> {
    
    private CachingService _service;
    
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;

    private static AssetBundleManifestInfo _bindInfo;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _manifestScrollViewPosition;

    protected override string CONFIG_NAME => $"{nameof(AssetBundleTesterConfig)}.json";
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";

    public override void Destroy() {
        base.Destroy();
    }

    public override void CacheRefresh() {
        _service ??= Service.GetService<CachingService>();
        
        if (Service.TryGetService<SystemWatcherService>(out var service)) {
            service.StartWatcher(watcherOrder);
        }

        if (JsonUtil.TryLoadJsonIgnoreLog(CONFIG_PATH, out config)) {
            _plainEncryptKey = string.IsNullOrEmpty(config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(config.cipherEncryptKey) : string.Empty;
            config.StartAutoSave(CONFIG_PATH);
        } else {
            if (config == null || config.IsNull() == false) {
                config = new AssetBundleTesterConfig.NullConfig();
            }
        }
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            GUILayout.Label("HTTP 서버 다운로드 설정", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope("box")) {
                config.downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", config.downloadDirectory);
                config.url = EditorCommon.DrawLabelTextField("URL", config.url, 120f);
                EditorCommon.DrawButtonPasswordField("암호화 키 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey), 120f);

                EditorCommon.DrawSeparator();

                using (new GUILayout.HorizontalScope()) {
                    GUILayout.Label("캐싱(Caching)", Constants.Editor.AREA_TITLE_STYLE, GUILayout.ExpandWidth(false));
                    if (config.isActiveCaching && GUILayout.Button("Caching Service 바로가기", GUILayout.ExpandWidth(false))) {
                        EditorCachingService.OpenWindow();
                    }
                }
                
                config.isActiveCaching = EditorCommon.DrawLabelToggle(config.isActiveCaching, "Caching 활성화", 100f);
                if (config.isActiveCaching && _service.IsReady()) {
                    EditorCommon.DrawLabelTextField("현재 활성화된 Caching 폴더", _service.Get().path, 170f);
                }
            }
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label($"{nameof(AssetBundleManifest)} 다운로드 테스트", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope("box")) {
                // TODO. AssetBundleManifest 선별 처리 UI
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isActiveCustomManifestPath, "임의의 경로 입력 활성화", 150f);
                    EditorCommon.DrawLabelToggle(ref config.isActiveLocalSave, "로컬 저장 활성화", 150f);
                }
                
                if (config.isActiveCustomManifestPath) {
                    EditorCommon.DrawLabelTextField("임의 경로", ref config.customManifestPath);
                } else {
                    EditorCommon.DrawEnumPopup($"{nameof(BuildTarget)}", ref config.selectBuildTarget);
                }
                
                EditorCommon.DrawLabelTextField("AssetBundleManifest 다운로드 경로", config.GetManifestPath(), 210f);
                EditorCommon.DrawLabelTextField("AssetBundleManifest CRC", ref config.crc, 210f);
                
                GUILayout.Space(5f);
                
                if (GUILayout.Button("Manifest 다운로드 테스트")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    DownloadAssetBundleManifest(config.GetManifestPath(), 0, callback:(result, manifest) => {
                        if (result == Result.Success && manifest != null) {
                            _bindInfo = new AssetBundleManifestInfo(manifest, Path.GetFileName(config.GetManifestPath()));
                        }
                    });
                }

                // TODO. 현재 다운로드 된 AssetBundleManifest 의 리스트 출력
                // TODO. 한 번에 여러 AssetBundleManifest 를 바인딩하고 출력하는 처리 추가
                if (_bindInfo != null && _bindInfo.IsValid()) {
                    EditorCommon.DrawSeparator();
                    _manifestScrollViewPosition = EditorGUILayout.BeginScrollView(_manifestScrollViewPosition, false, false, GUILayout.Height(150f));
                    using (new GUILayout.VerticalScope()) {
                        foreach (var name in _bindInfo.manifest.GetAllAssetBundles()) {
                            if (GUILayout.Button(name)) {
                                AssetBundle.UnloadAllAssetBundles(true);
                                if (config.isActiveLocalSave) {
                                    // TODO. 로컬 저장 시 캐싱되지 않으며 UnityWebRequest를 통해 처리 및 암호화 체크
                                } else if (config.isActiveCaching) {
                                    // TODO. UnityWebRequestAssetBundle을 통해 Caching 처리
                                } else {
                                    // TODO. 로컬 세이브만 끄면 동일함 manifest 다운로드와 동일함
                                }
                                
                                DownloadAssetBundle(Path.Combine(config.selectBuildTarget.ToString(), name), _bindInfo.manifest.GetAssetBundleHash(name), callback:(result, _) => {
                                    Logger.TraceLog($"{name} || {result}", Color.cyan);
                                });
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                
                EditorCommon.DrawSeparator();
                
                // TODO. 선택된 AssetBundleManifest 대한 전체 혹은 일부 AssetBundle 다운로드로 변경
                // TODO. Toggle을 통한 구현이 가능
                if (GUILayout.Button("AssetBundle 다운로드 테스트(비암호화)")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    if (_bindInfo != null && _bindInfo.IsValid()) {
                        foreach (var assetBundleName in _bindInfo.manifest.GetAllAssetBundles()) {
                            var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                            DownloadAssetBundle(assetBundlePath, _bindInfo.manifest.GetAssetBundleHash(assetBundleName), callback:(result, assetBundle) => {
                                if (result != Result.Success && assetBundle == null) {
                                    DownloadAssetBundle(assetBundlePath, callback:(result, _) => Logger.TraceError(result));
                                }
                            });
                        }
                    }
                }

                // TODO. 어떤 식으로 구현할지 검토
                if (GUILayout.Button("AssetBundle 다운로드 테스트(암호화)")) {
                    
                }
            }
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    private void DownloadAssetBundleManifest(string name, uint crc = 0, Action<Result, AssetBundleManifest> callback = null) {
        var request = AssetBundleManifestDownloadHandler.CreateUnityWebRequest(Path.Combine(config.url, name), crc, _plainEncryptKey);
        request.SendWebRequest().completed += _ => {
            if (request.responseCode != (long) HttpStatusCode.OK) {
                Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
                return;
            }
            
            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive(request.error);
                callback?.Invoke(request.result, null);
            } else {
                try {
                    if (AssetBundleManifestDownloadHandler.TryGetContent(request, out var manifest)) {
                        if (config.isActiveLocalSave) {
                            File.WriteAllBytes(config.GetManifestDownloadPath(), request.downloadHandler.data);
                        }
                        
                        callback?.Invoke(request.result, manifest);
                    }
                } catch (Exception ex) {
                    Logger.TraceError(ex);
                }
            }
        };
    }

    private void DownloadAssetBundle(string name, Hash128 hash, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), hash, crc);
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceError(request.error);
                callback?.Invoke(request.result, null);
            } else {
                try {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (assetBundle != null) {
                        callback?.Invoke(request.result, assetBundle);
                    }
                } catch (Exception ex) {
                    Logger.TraceLog(ex, Color.red);
                }
            }
        };
    }

    private void DownloadAssetBundle(string name, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var path = Path.Combine(config.url, name);
        var request = UnityWebRequest.Get(path);
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceError(request.error);
                callback?.Invoke(request.result, null);
            } else {
                try {
                    var assetBundle = AssetBundle.LoadFromMemory(request.downloadHandler.data, crc) ?? AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data, _plainEncryptKey), crc);
                    if (assetBundle != null) {
                        callback?.Invoke(request.result, assetBundle);
                    }
                } catch (Exception ex) {
                    Logger.TraceLog(ex, Color.red);
                }
            }
        };
    }
}

public class AssetBundleTesterConfig : JsonAutoConfig {
    
    public class NullConfig : AssetBundleTesterConfig { }

    public string downloadDirectory; 
    public bool isActiveCaching;
    public string url;
    public string cipherEncryptKey;

    public bool isActiveCustomManifestPath;
    public bool isActiveLocalSave;
    
    public string customManifestPath;
    public BuildTarget selectBuildTarget = EditorUserBuildSettings.activeBuildTarget;
    public string crc;
    
    public string GetManifestDownloadPath() => $"{downloadDirectory}/{selectBuildTarget}";
    public string GetManifestPath() => isActiveCustomManifestPath ? customManifestPath : $"{selectBuildTarget}/{selectBuildTarget}";
    
    public override bool IsNull() => this is NullConfig;
}

public record AssetBundleManifestInfo {
    
    public AssetBundleManifest manifest;
    public string name;

    public AssetBundleManifestInfo(AssetBundleManifest manifest, string name) {
        this.manifest = manifest;
        this.name = name;
    } 

    public bool IsValid() => manifest != null;
}

public sealed class AssetBundleManifestDownloadHandler : DownloadHandlerScript {

    public readonly string url;
    public readonly Hash128? hash;
    public readonly uint crc;
    public readonly string encryptKey;

    private byte[] _bytes;

    private DownloadHandlerAssetBundle _test;

    public AssetBundleManifestDownloadHandler(uint crc, string encryptKey) {
        this.crc = crc;
        this.encryptKey = encryptKey;
    }
    
    public AssetBundleManifestDownloadHandler(string url, uint crc, string encryptKey) : this(crc, encryptKey) => this.url = url;
    public AssetBundleManifestDownloadHandler(string url, Hash128 hash, uint crc, string encryptKey) : this(url, crc, encryptKey) => this.hash = hash;

    protected override byte[] GetData() => _bytes;

    protected override bool ReceiveData(byte[] data, int dataLength) {
        _bytes = data;
        return base.ReceiveData(data, dataLength);
    }

    public static UnityWebRequest CreateUnityWebRequest(string path, uint crc = 0, string encryptKey = "") => new(path, "GET", new AssetBundleManifestDownloadHandler(path, crc, encryptKey), null);
    public static UnityWebRequest CreateUnityWebRequest(string path, Hash128 hash, uint crc = 0, string encryptKey = "") => new UnityWebRequest(path, "GET", new AssetBundleManifestDownloadHandler(path, hash, crc, encryptKey), null);

    // TODO. Caching이 필요 없는 경우 에셋번들 다운로드를 처리해야 하기 때문에 에셋번들을 반환하도록 다시 수정
    public static bool TryGetContent(UnityWebRequest request, out AssetBundleManifest manifest) {
        manifest = GetContent(request);
        return manifest != null;
    }
    
    
    // TODO. Caching이 필요 없는 경우 에셋번들 다운로드를 처리해야 하기 때문에 에셋번들을 반환하도록 다시 수정
    public static AssetBundleManifest GetContent(UnityWebRequest request) {
        var handler = GetCheckedDownloader<AssetBundleManifestDownloadHandler>(request);
        if (handler != null) {
            var assetBundle = AssetBundle.LoadFromMemory(handler.data, handler.crc) ?? AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(handler.data, handler.encryptKey), handler.crc);
            if (assetBundle != null && assetBundle.TryGetManifest(out var manifest)) {
                assetBundle.Unload(false);
                return manifest;
            }
        }

        return null;
    }
}