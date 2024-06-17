using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Test, typeof(AssetBundleProvider))]
public class EditorAssetBundleTesterDrawer : EditorResourceDrawerAutoConfig<AssetBundleTesterConfig, AssetBundleTesterConfig.NullConfig> {
    
    private CachingService _service;
    
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;

    private static AssetBundleManifest _bindAssetBundleManifest;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _manifestScrollViewPosition;

    protected override string CONFIG_NAME => $"{nameof(AssetBundleTesterConfig)}.json";
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";

    public override void Destroy() {
        _bindAssetBundleManifest = null;
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
            using (new GUILayout.VerticalScope("box")) {
                GUILayout.Label("HTTP 서버 다운로드 테스트", Constants.Editor.AREA_TITLE_STYLE);
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
                
                GUILayout.Space(5f);
                
            }
            
            using (new GUILayout.VerticalScope("box")) {
                // TODO. AssetBundleManifest 선별 처리 UI
                if (GUILayout.Button("Manifest 다운로드 테스트")) {
                    AssetBundle.UnloadAllAssetBundles(true);
                    DownloadAssetBundleManifest($"{EditorUserBuildSettings.activeBuildTarget}/{EditorUserBuildSettings.activeBuildTarget}", manifest => {
                        // TODO. 변경된 실제 파일 이름과 AssetBundleManifest 와 결합된 형태의 데이터를 바인딩 하도록 수정
                        _bindAssetBundleManifest = manifest;
                    });
                }
                
                // TODO. 현재 다운로드 된 AssetBundleManifest 의 리스트 출력
                // TODO. 한 번에 여러 AssetBundleManifest 를 바인딩하고 출력하는 처리 추가
                if (_bindAssetBundleManifest != null) {
                    _manifestScrollViewPosition = EditorGUILayout.BeginScrollView(_manifestScrollViewPosition, false, false);
                    using (new GUILayout.VerticalScope()) {
                        foreach (var name in _bindAssetBundleManifest.GetAllAssetBundles()) {
                            if (GUILayout.Button(name)) {
                                // TODO. 에셋번들 개별 다운로드
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
                    DownloadAssetBundleManifest($"{EditorUserBuildSettings.activeBuildTarget}/{EditorUserBuildSettings.activeBuildTarget}", manifest => {
                        foreach (var assetBundleName in manifest.GetAllAssetBundles()) {
                            var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                            DownloadAssetBundle(assetBundlePath, manifest.GetAssetBundleHash(assetBundleName), assetBundle => {
                                Logger.TraceError($"{assetBundle.name} || {manifest.GetAssetBundleHash(assetBundle.name)}");
                            });
                        }
                    });
                }
            }

            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }
    
    private void DownloadAssetBundleManifest(string name, Action<AssetBundleManifest> callback = null) {
        var request = UnityWebRequest.Get(Path.Combine(config.url, name));
        Logger.TraceLog($"Request || {request.url}", Color.cyan);
        request.SendWebRequest().completed += _ => {
            if (request.result != UnityWebRequest.Result.Success) {
                Logger.TraceErrorExpensive(request.error);
            } else {
                if (request.responseCode != (long) HttpStatusCode.OK) {
                    Logger.TraceError($"Already ResponseCode || {request.responseCode}");
                    return;
                }

                try {
                    var assetBundle = AssetBundle.LoadFromMemory(request.downloadHandler.data);
                    if (assetBundle == null) {
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data, _plainEncryptKey));
                    }

                    if (assetBundle != null) {
                        foreach (var assetBundleName in assetBundle.GetAllAssetNames()) {
                            if (assetBundleName.Contains("manifest")) {
                                var manifest = assetBundle.LoadAsset<AssetBundleManifest>(assetBundleName);
                                if (manifest != null) {
                                    callback?.Invoke(manifest);
                                }
                                break;
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.TraceError($"{nameof(AssetBundle)} Resource download was successful, but memory load failed. It appears to be either an incorrect format or a decryption failure. The issue might be related to the encryption key.\n\n{ex}");
                }
            }
        };
    }
    
    private void DownloadAssetBundle(string name, Hash128 hash, Action<AssetBundle> callback = null) {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), hash);
        Logger.TraceLog($"Request || {request.url}", Color.cyan);
        request.SendWebRequest().completed += operation => {
            if (request.result != UnityWebRequest.Result.Success) {
                Logger.TraceError(request.error);
            } else {
                try {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (assetBundle == null) {
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data, _plainEncryptKey));
                    }
                    
                    if (assetBundle != null) {
                        callback?.Invoke(assetBundle);
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

    public override bool IsNull() => this is NullConfig;
}
