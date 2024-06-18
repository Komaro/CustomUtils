using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
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
                
                GUILayout.Space(5f);
                
                if (GUILayout.Button("Manifest 다운로드 테스트")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    DownloadAssetBundleManifest(config.GetManifestPath(), (_, manifest) => {
                        _bindInfo ??= new AssetBundleManifestInfo();
                        _bindInfo.manifest = manifest;
                        _bindInfo.name = Path.GetFileName(config.GetManifestPath());
                    });
                }

                if (GUILayout.Button("Manifest CRC 다운로드 테스트")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    DownloadAssetBundleManifest(config.GetManifestPath(), 4209180446, (_, manifest) => {
                        if (manifest == null) {
                            DownloadAssetBundleManifest(config.GetManifestPath());
                        }
                    });
                }
                
                // TODO. 현재 다운로드 된 AssetBundleManifest 의 리스트 출력
                // TODO. 한 번에 여러 AssetBundleManifest 를 바인딩하고 출력하는 처리 추가
                if (_bindInfo != null && _bindInfo.IsValid()) {
                    _manifestScrollViewPosition = EditorGUILayout.BeginScrollView(_manifestScrollViewPosition, false, false, GUILayout.Height(150f));
                    using (new GUILayout.VerticalScope()) {
                        foreach (var name in _bindInfo.manifest.GetAllAssetBundles()) {
                            if (GUILayout.Button(name)) {
                                AssetBundle.UnloadAllAssetBundles(true);
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
                if (GUILayout.Button("AssetBundle 다운로드 테스트")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    if (_bindInfo != null && _bindInfo.IsValid()) {
                        foreach (var assetBundleName in _bindInfo.manifest.GetAllAssetBundles()) {
                            var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                            DownloadAssetBundle(assetBundlePath, _bindInfo.manifest.GetAssetBundleHash(assetBundleName), callback:(_, assetBundle) => {
                                // Caching.MarkAsUsed(Path.Combine(_service.Get().path, assetBundle.name), _bindInfo.manifest.GetAssetBundleHash(assetBundleName));
                            });
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    private void DownloadAssetBundleManifest(string name, uint crc, Action<Result, AssetBundleManifest> callback = null) {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), crc);
        request.SendWebRequest().completed += _ => {
            if (request.responseCode != (long) HttpStatusCode.OK) {
                Logger.TraceError($"Already ResponseCode || {request.responseCode}");
                return;
            }
            
            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive(request.error);
                Logger.TraceError("When the AssetBundleManifest is encrypted, CRC checksum verification cannot be applied.");
                callback?.Invoke(request.result, null);
            } else {
                var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                if (assetBundle != null && assetBundle.TryGetManifest(out var manifest)) {
                    if (config.isActiveLocalSave) {
                        File.WriteAllBytes(config.GetManifestDownloadPath(), request.downloadHandler.data);
                    }
                    
                    assetBundle.Unload(false);
                    callback?.Invoke(request.result, manifest);
                }
            }
        };
    }
    
    private void DownloadAssetBundleManifest(string name, Action<Result, AssetBundleManifest> callback = null) {
        var request = UnityWebRequest.Get(Path.Combine(config.url, name));
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive(request.error);
                callback?.Invoke(request.result, null);
            } else {
                if (request.responseCode != (long) HttpStatusCode.OK) {
                    Logger.TraceError($"Already ResponseCode || {request.responseCode}");
                    return;
                }

                try {
                    var assetBundle = AssetBundle.LoadFromMemory(request.downloadHandler.data) ?? AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data, _plainEncryptKey));
                    if (assetBundle != null && assetBundle.TryGetManifest(out var manifest)) {
                        if (config.isActiveLocalSave) {
                            File.WriteAllBytes(config.GetManifestDownloadPath(), request.downloadHandler.data);
                        }
                        
                        assetBundle.Unload(false);
                        callback?.Invoke(request.result, manifest);
                    }
                } catch (Exception ex) {
                    Logger.TraceError($"{nameof(AssetBundle)} Resource download was successful, but memory load failed. It appears to be either an incorrect format or a decryption failure. The issue might be related to the encryption key.\n\n{ex}");
                }
            }
        };
    }
    
    // TODO. 에셋번들 로컬 다운로드를 구현하기 위해서는 hash 적용이 어려움, hash 적용 자체가 Caching 처리를 지원함

    private void DownloadAssetBundle(string name, Hash128 hash, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), hash, crc);
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceError(request.error);
                callback?.Invoke(request.result, null);
            } else {
                try {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(request) ?? AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data, _plainEncryptKey));
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
    
    public string GetManifestDownloadPath() => $"{downloadDirectory}/{selectBuildTarget}";
    public string GetManifestPath() => isActiveCustomManifestPath ? customManifestPath : $"{selectBuildTarget}/{selectBuildTarget}";
    
    public override bool IsNull() => this is NullConfig;
}

public record AssetBundleManifestInfo {
    
    public AssetBundleManifest manifest;
    public string name;
    
    public bool IsValid() => manifest != null;
}
