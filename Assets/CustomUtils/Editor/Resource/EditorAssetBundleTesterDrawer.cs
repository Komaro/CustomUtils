using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Test, typeof(AssetBundleProvider))]
public partial class EditorAssetBundleTesterDrawer : EditorResourceDrawer<AssetBundleTesterConfig, AssetBundleTesterConfig.NullConfig> {
    
    private CachingService _cachingService;
    private DownloadService _downloadService;
    
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;

    private bool _checksumInfoFold;

    private HashSet<string> _ensureUniqueSet = new();
    private ConcurrentQueue<AsyncProgressOperation> _downloadQueue = new();

    private static AssetBundleChecksumInfo _bindChecksumInfo;
    private static AssetBundleManifestInfo _bindManifestInfo;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _manifestScrollViewPosition;
    private Vector2 _downloadQueueScrollViewPosition;

    private readonly SystemWatcherServiceOrder _infoAutoTrackingOrder;

    protected override string CONFIG_NAME => $"{nameof(AssetBundleTesterConfig)}{Constants.Extension.JSON}";

    public EditorAssetBundleTesterDrawer(EditorWindow window) : base(window) {
        _infoAutoTrackingOrder = new(Constants.Extension.JSON_FILTER) {
            filters = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            includeSubDirectories = true,
            handler = OnFileSystemEventHandler
        };
    }
    
    public override void CacheRefresh() {
        Service.GetService<SystemWatcherService>().Start(order);
        _cachingService ??= Service.GetService<CachingService>();
        _downloadService ??= Service.GetService<DownloadService>();
        
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

        if (_downloadQueue.IsEmpty == false) {
            while (_downloadQueue.TryDequeue(out var result)) {
                result.Dispose();
            }
        }
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("HTTP 서버 다운로드 설정", Constants.Draw.AREA_TITLE_STYLE);
                if (GUILayout.Button(Constants.Draw.SHORT_CUT_ICON, Constants.Draw.FIT_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                    EditorHttpWebServerService.OpenWindow();
                }
                GUILayout.FlexibleSpace();
            }
            
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawEnumPopup($"빌드 타겟", ref config.selectBuildTarget);
                EditorCommon.DrawLabelTextField("URL", ref config.url, 120f);
                EditorCommon.DrawFolderOpenSelector("다운로드 폴더", "선택", ref config.downloadDirectory, 120f);
                EditorCommon.DrawButtonPasswordField("암호화 키 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey), 120f);
            }
            
            EditorCommon.DrawSeparator();

            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("캐싱(Caching)", Constants.Draw.AREA_TITLE_STYLE, GUILayout.ExpandWidth(false));
                if (config.isActiveCaching && GUILayout.Button(Constants.Draw.SHORT_CUT_ICON, Constants.Draw.FIT_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                    EditorCachingService.OpenWindow();
                }
            }
            
            using (new GUILayout.VerticalScope("box")) {
                EditorCommon.DrawLabelToggle(ref config.isActiveCaching, "Caching 활성화", 100f);
                if (config.isActiveCaching && _cachingService.IsReady()) {
                    EditorCommon.DrawLabelTextField("현재 활성화된 Caching 폴더", _cachingService.Get().path, 170f);
                    EditorGUILayout.HelpBox("Caching이 활성화 되어 있는 경우 일반 다운로드 폴더 지정이 무시됩니다", MessageType.Warning);
                }
            }
            
            EditorCommon.DrawSeparator();
            DrawChecksum();
            
            EditorCommon.DrawSeparator();
            DrawDownload();
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }
    
    private record AssetBundleManifestInfo {
    
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
}

public class AssetBundleTesterConfig : JsonCoroutineAutoConfig {

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