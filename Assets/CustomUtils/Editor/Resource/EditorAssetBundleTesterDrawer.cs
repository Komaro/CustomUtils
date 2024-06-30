using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Test, typeof(AssetBundleProvider))]
public class EditorAssetBundleTesterDrawer : EditorResourceDrawerAutoConfig<AssetBundleTesterConfig, AssetBundleTesterConfig.NullConfig> {
    
    private CachingService _service;
    
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;

    private bool _checksumInfoFold;

    private static AssetBundleChecksumInfo _bindChecksumInfo;
    private static AssetBundleManifestInfo _bindManifestInfo;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _manifestScrollViewPosition;

    private readonly SystemWatcherServiceOrder _infoAutoTrackingOrder;

    protected override string CONFIG_NAME => $"{nameof(AssetBundleTesterConfig)}{Constants.Extension.JSON}";
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";

    public EditorAssetBundleTesterDrawer() {
        _infoAutoTrackingOrder = new(Constants.Extension.JSON_FILTER) {
            filters = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            includeSubDirectories = true,
            handler = OnFileSystemEventHandler
        };
    }
    
    public override void CacheRefresh() {
        Service.GetService<SystemWatcherService>().Start(watcherOrder);
        _service ??= Service.GetService<CachingService>();
        
        if (JsonUtil.TryLoadJson(CONFIG_PATH, out config)) {
            _plainEncryptKey = string.IsNullOrEmpty(config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(config.cipherEncryptKey) : string.Empty;
            config.StartAutoSave(CONFIG_PATH);
        } else {
            if (config == null || config.IsNull() == false) {
                config = new AssetBundleTesterConfig.NullConfig();
            }
        }
        
        if (config.isActiveChecksum) {
            if (config.isActiveAutoTrackingChecksum && _bindChecksumInfo == null) {
                SearchLatestChecksumInfo();
                StartChecksumInfoAutoSearch();
            } else if (string.IsNullOrEmpty(config.checksumInfoPath) == false) {
                LoadAssetBundleChecksumInfo(config.checksumInfoPath);
            }
        }
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("HTTP 서버 다운로드 설정", Constants.Editor.AREA_TITLE_STYLE);
                if (GUILayout.Button(Constants.Editor.SHORT_CUT_ICON, Constants.Editor.FIT_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                    EditorHttpWebServerService.OpenWindow();
                }
                GUILayout.FlexibleSpace();
            }
            
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawEnumPopup($"빌드 타겟", ref config.selectBuildTarget);
                EditorCommon.DrawLabelTextField("URL", ref config.url, 120f);
                EditorCommon.DrawFolderOpenSelector("다운로드 폴더", "선택", ref config.downloadDirectory);
                EditorCommon.DrawButtonPasswordField("암호화 키 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey), 120f);
            }
            
            EditorCommon.DrawSeparator();

            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("캐싱(Caching)", Constants.Editor.AREA_TITLE_STYLE, GUILayout.ExpandWidth(false));
                if (config.isActiveCaching && GUILayout.Button(Constants.Editor.SHORT_CUT_ICON, Constants.Editor.FIT_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                    EditorCachingService.OpenWindow();
                }
            }
            
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawLabelToggle(ref config.isActiveCaching, "Caching 활성화", 100f);
                if (config.isActiveCaching && _service.IsReady()) {
                    EditorCommon.DrawLabelTextField("현재 활성화된 Caching 폴더", _service.Get().path, 170f);
                }
            }
            
            EditorCommon.DrawSeparator();

            GUILayout.Label("체크썸(Checksum)", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawLabelToggle(ref config.isActiveChecksum, "Checksum 활성화", 150f);
                
                if (config.isActiveChecksum) {
                    using (new GUILayout.HorizontalScope()) {
                        EditorCommon.DrawLabelToggle(ref config.isActiveCrc, "CRC 체크 활성화", 150f);
                        EditorCommon.DrawLabelToggle(ref config.isActiveHash, "Hash 체크 활성화", 150f);
                        GUILayout.FlexibleSpace();
                    }

                    using (new GUILayout.VerticalScope()) {
                        EditorGUI.BeginChangeCheck();
                        using (new GUILayout.HorizontalScope()) {
                            EditorCommon.DrawLabelToggle(ref config.isActiveAutoTrackingChecksum, "Checksum 자동 탐색", 150f);
                            if (config.isActiveAutoTrackingChecksum) {
                                EditorCommon.DrawLabelToggle(ref config.isActiveAllDirectoriesSearch, "하위폴더 탐색 활성화", 150f);
                            }
                            GUILayout.FlexibleSpace();
                        }

                        if (config.isActiveAutoTrackingChecksum) {
                            EditorCommon.DrawFolderOpenSelector("탐색 폴더", "선택", ref config.checksumInfoTrackingDirectory);
                            EditorCommon.DrawLabelTextField("Json 파일 경로", config.checksumInfoPath, 120f);
                        }
                        
                        if (EditorGUI.EndChangeCheck()) {
                            if (config.isActiveAutoTrackingChecksum && Directory.Exists(config.checksumInfoTrackingDirectory)) {
                                SearchLatestChecksumInfo();
                                StartChecksumInfoAutoSearch();
                            } else {
                                StopChecksumInfoAutoSearch();
                            }
                        }

                        if (config.isActiveAutoTrackingChecksum == false) {
                            EditorCommon.DrawFileOpenSelector(ref config.checksumInfoPath, "로컬 Json 파일", "선택", "json", () => LoadAssetBundleChecksumInfo(config.checksumInfoPath));
                            EditorCommon.DrawButtonTextField("Checksum Info Json 다운로드", ref config.checksumInfoDownloadPath, () => {
                                config.checksumInfoDownloadPath = config.checksumInfoDownloadPath.AutoSwitchExtension(Constants.Extension.JSON);
                                DownloadJsonFile<AssetBundleChecksumInfo>(config.checksumInfoDownloadPath, (result, info) => {
                                    if (result == Result.Success && info != null) {
                                        _bindChecksumInfo = info;
                                    }
                                });
                            });
                        }
                    }
                }
            }
            
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawLabelTextField("연결 상태", _bindChecksumInfo != null ? "연결".GetColorString(Color.green) : "미연결".GetColorString(Color.red));
                if (_bindChecksumInfo != null) {
                    EditorCommon.DrawLabelTextField("생성 시간", _bindChecksumInfo.generateTime.ToString(CultureInfo.CurrentCulture));
                    _checksumInfoFold = EditorGUILayout.BeginFoldoutHeaderGroup(_checksumInfoFold, string.Empty);
                    if (_checksumInfoFold) {
                        foreach (var pair in _bindChecksumInfo.crcDic) {
                            EditorCommon.DrawLabelTextField(pair.Key, pair.Value.ToString(), 200f);
                        }
                        
                        EditorCommon.DrawSeparator();
                        
                        foreach (var  pair in _bindChecksumInfo.hashDic) {
                            EditorCommon.DrawLabelTextField(pair.Key, pair.Value, 200f);
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label($"{nameof(AssetBundleManifest)} 다운로드 테스트", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope("box")) {
                // TODO. AssetBundleManifest 선별 처리 UI
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
                    if (SystemUtil.TryReadAllBytes(config.localManifestPath, out var bytes)) {
                        AssetBundle.UnloadAllAssetBundles(false);
                        if (AssetBundleUtil.TryLoadFromMemoryOrDecrypt(bytes, _plainEncryptKey, out var assetBundle) && assetBundle.TryFindManifest(out var manifest)) {
                            _bindManifestInfo = new AssetBundleManifestInfo(manifest, Path.GetFileName(config.localManifestPath));
                        }
                    }
                });
                
                if (GUILayout.Button("Manifest 다운로드")) {
                    AssetBundle.UnloadAllAssetBundles(false);
                    DownloadAssetBundleManifest(config.GetManifestPath(), 0, callback:(result, manifest) => {
                        if (result == Result.Success && manifest != null) {
                            _bindManifestInfo = new AssetBundleManifestInfo(manifest, Path.GetFileName(config.GetManifestPath()));
                        }
                    });
                }
                
                EditorCommon.DrawSeparator();

                using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                    EditorCommon.DrawLabelTextField("연결 상태", _bindManifestInfo?.IsValid() ?? false ? "연결".GetColorString(Color.green) : "미연결".GetColorString(Color.red));
                    if (_bindManifestInfo?.IsValid() ?? false) {
                        EditorCommon.DrawLabelTextField("로드 시간", _bindManifestInfo.loadTime.ToString(CultureInfo.CurrentCulture));
                        
                        _manifestScrollViewPosition = EditorGUILayout.BeginScrollView(_manifestScrollViewPosition, false, false, GUILayout.Height(150f));
                        using (new GUILayout.VerticalScope()) {
                            foreach (var name in _bindManifestInfo.manifest.GetAllAssetBundles()) {
                                using (new GUILayout.HorizontalScope()) {
                                    EditorGUILayout.LabelField(name);
                                    if (GUILayout.Button("다운로드")) {
                                        AssetBundle.UnloadAllAssetBundles(false);
                                        var path = $"{config.selectBuildTarget}/{name}";
                                        if (config.isActiveCaching) {
                                            DownloadAssetBundle(path, _bindManifestInfo.manifest.GetAssetBundleHash(name), callback:OnAssetBundleDownloadComplete);
                                        } else {
                                            DownloadAssetBundle(path, callback:OnAssetBundleDownloadComplete);
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
                                    DownloadAssetBundle(path, _bindManifestInfo.manifest.GetAssetBundleHash(assetBundleName), callback:OnAssetBundleDownloadComplete);
                                }
                            } else {
                                DownloadAssetBundle(path, callback:OnAssetBundleDownloadComplete);
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
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    private void DownloadJsonFile<T>(string serverPath, Action<Result, T> callback = null) {
        var request = JsonDownloadHandler.CreateUnityWebRequest(Path.Combine(config.url, serverPath), _plainEncryptKey);
        request.SendWebRequest().completed += _ => {
            if (request.responseCode != (long) HttpStatusCode.OK) {
                Logger.TraceErrorExpensive($"Already ResponseCode || {request.responseCode}");
                return;
            }

            if (request.result != Result.Success) {
                Logger.TraceErrorExpensive(request.error);
                callback?.Invoke(request.result, default);
            } else {
                try {
                    if (JsonDownloadHandler.TryGetContent<T>(request, out var info)) {
                        callback?.Invoke(request.result, info);
                    }
                } catch (Exception ex) {
                    Logger.TraceError(ex);
                }
            }
        };
    }

    private void DownloadAssetBundleManifest(string name, uint crc = 0, Action<Result, AssetBundleManifest> callback = null) {
        var request = AssetBundleDownloadHandler.CreateUnityWebRequest(Path.Combine(config.url, name), crc, _plainEncryptKey);
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
                    if (AssetBundleDownloadHandler.TryGetContent(request, out var assetBundle) && assetBundle.TryFindManifest(out var manifest)) {
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

    private void DownloadAssetBundle(string name, Hash128? hash, uint crc = 0, Action<Result, AssetBundle> callback = null) {
        var request = hash.HasValue ? UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), hash.Value, crc) : UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(config.url, name), crc);
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
        var request = AssetBundleDownloadHandler.CreateUnityWebRequest(Path.Combine(config.url, name), crc, _plainEncryptKey);
        request.SendWebRequest().completed += _ => {
            if (request.result != Result.Success) {
                Logger.TraceError(request.error);
                callback?.Invoke(request.result, null);
            } else {
                try {
                    if (AssetBundleDownloadHandler.TryGetContent(request, out var assetBundle)) {
                        if (config.isActiveLocalSave) {
                            File.WriteAllBytes($"{config.downloadDirectory}/{assetBundle.name}", request.downloadHandler.data);
                        }
                        
                        callback?.Invoke(request.result, assetBundle);
                    }
                } catch (Exception ex) {
                    Logger.TraceLog(ex, Color.red);
                }
            }
        };
    }

    private void LoadAssetBundleChecksumInfo(string path) {
        if (JsonUtil.TryLoadJsonOrDecrypt<AssetBundleChecksumInfo>(out var info, path, _plainEncryptKey)) {
            _bindChecksumInfo = info;
            config.checksumInfoPath = path;
        } else {
            Logger.TraceError($"{path} is Invalid {nameof(AssetBundleChecksumInfo)}");
        }
    }
    
    private void SearchLatestChecksumInfo() {
        if (Directory.Exists(config.checksumInfoTrackingDirectory)) {
            var filePaths = Directory.GetFiles(config.checksumInfoTrackingDirectory, Constants.Extension.JSON_FILTER, config.isActiveAllDirectoriesSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (filePaths.Any()) {
                var path = filePaths.OrderByDescending(File.GetLastWriteTime).First();
                LoadAssetBundleChecksumInfo(path);
            }
        }
    }

    private void StartChecksumInfoAutoSearch() {
        if (Service.TryGetService<SystemWatcherService>(out var service)) {
            service.Remove(_infoAutoTrackingOrder);
            _infoAutoTrackingOrder.path = config.checksumInfoTrackingDirectory;
            service.Start(_infoAutoTrackingOrder);
        }
    }

    private void StopChecksumInfoAutoSearch() {
        if (Service.TryGetService<SystemWatcherService>(out var service)) {
            service.Stop(_infoAutoTrackingOrder);
        }
    }

    private void OnFileSystemEventHandler(object _, FileSystemEventArgs args) {
        switch (args.ChangeType) {
            case WatcherChangeTypes.Deleted:
                if (config.checksumInfoPath.EqualsFast(args.FullPath)) {
                    SearchLatestChecksumInfo();
                }
                break;
            default:
                if (_bindChecksumInfo == null) {
                    LoadAssetBundleChecksumInfo(args.FullPath);
                } else {
                    if (_bindChecksumInfo.generateTime > File.GetCreationTime(args.FullPath)) {
                        LoadAssetBundleChecksumInfo(args.FullPath);
                    }
                }
                break;
        }
    }

    private void OnAssetBundleDownloadComplete(Result result, AssetBundle assetBundle) {
        switch (result) {
            case Result.Success:
                Logger.TraceLog($"{assetBundle.name} Download Success");
                break;
            case Result.DataProcessingError:
                Logger.TraceError("The asset bundle may be encrypted. Please check the asset bundle build options again.");
                break;
        }
    }
}

public class AssetBundleTesterConfig : JsonAutoConfig {

    public BuildTarget selectBuildTarget = EditorUserBuildSettings.activeBuildTarget;
    public string downloadDirectory; 
    public string url;
    public string cipherEncryptKey;
    
    public bool isActiveCaching;
    public bool isActiveChecksum;
    public bool isActiveAutoTrackingChecksum;
    public bool isActiveAllDirectoriesSearch;
    public bool isActiveCrc;
    public bool isActiveHash;
    public string checksumInfoPath;
    public string checksumInfoTrackingDirectory;
    public string checksumInfoDownloadPath;

    public bool isActiveCustomManifestPath;
    public bool isActiveLocalSave;

    public string localManifestPath;
    public string customManifestPath;
    
    public string GetManifestDownloadPath() => $"{downloadDirectory}/{selectBuildTarget}";
    public string GetManifestPath() => isActiveCustomManifestPath ? customManifestPath : $"{selectBuildTarget}/{selectBuildTarget}";
    
    public override bool IsNull() => this is NullConfig;
    public class NullConfig : AssetBundleTesterConfig { }
}

public record AssetBundleManifestInfo {
    
    public AssetBundleManifest manifest;
    public string name;
    public DateTime loadTime;

    public AssetBundleManifestInfo(AssetBundleManifest manifest, string name) {
        this.manifest = manifest;
        this.name = name;
        loadTime = DateTime.Now;
    }

    public bool IsValid() => manifest != null;
}