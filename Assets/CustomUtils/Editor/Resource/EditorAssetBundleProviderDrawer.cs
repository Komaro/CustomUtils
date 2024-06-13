using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[EditorResourceProviderDrawer(typeof(AssetBundleProvider))]
public class EditorAssetBundleProviderDrawer : EditorResourceDrawer {
    
    private CachingService _service;
    
    private Config _config;
    private static string _configPath;

    private bool _isShowEncryptKey = false;
    private BuildTarget _selectBuildTarget = EditorUserBuildSettings.activeBuildTarget;
    
    private static string _plainEncryptKey;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _selectAssetBundleScrollViewPosition;
    private Vector2 _buildOptionScrollViewPosition;

    private List<string> _allAssetBundleList = new();
    private SystemWatcherServiceOrder _watcherOrder;
    
    private const string CONFIG_NAME = "AssetBundleProviderConfig.json";
    private readonly List<BuildAssetBundleOptions> BUILD_OPTION_LIST;
    private readonly Regex NAME_REGEX = new(@"^[^\\/:*?""<>|]+$");
    private readonly GUIContent REFRESH_ICON = new(string.Empty, EditorGUIUtility.IconContent("d_Refresh").image);
    
    public EditorAssetBundleProviderDrawer() {
        _watcherOrder = new SystemWatcherServiceOrder(Constants.Editor.COMMON_CONFIG_FOLDER, CONFIG_NAME, OnSystemWatcherEventHandler);
        BUILD_OPTION_LIST = EnumUtil.GetValues<BuildAssetBundleOptions>(true, true).ToList();
    }
    
    public override void Close() {
        if (_config != null && _config.IsNull()) {
            _config.StopAutoSave();
            _config.Save(_configPath);
        }
        
        Service.GetService<SystemWatcherService>().StopWatcher(_watcherOrder);
    }
    
    public override void Destroy() {
        if (_watcherOrder != null) {
            Service.GetService<SystemWatcherService>().RemoveWatcher(_watcherOrder);
        }
    }

    public sealed override void CacheRefresh() {
        Service.GetService<SystemWatcherService>().StartWatcher(_watcherOrder);
        
        _service ??= Service.GetService<CachingService>();
        
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }
        
        if (File.Exists(_configPath) && JsonUtil.TryLoadJson(_configPath, out _config)) {
            _plainEncryptKey = string.IsNullOrEmpty(_config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(_config.cipherEncryptKey) : string.Empty;
            _config.StartAutoSave(_configPath);
        } else {
            if (_config == null || _config.IsNull() == false) {
                _config = new NullConfig();
            }
        }
        
        RefreshAssetBundleList();
        BUILD_OPTION_LIST.ForEach(option => _config.buildOptionDic.TryAdd(option, false));
    }

    public override void Draw() {
        if (_config?.IsNull() ?? true) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 AssetBundle 설정이 저장되지 않으며 일부 기능을 사용할 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button($"{nameof(Config)} 파일 생성")) {
                EditorCommon.OpenCheckDialogue($"{nameof(Config)} 파일 생성", $"{nameof(Config)} 파일을 생성합니다.\n경로는 아래와 같습니다.\n{_configPath}", ok: () => {
                    if ((_config = _config.Clone<Config>()) != null) {
                        _config.Save(_configPath);
                        _config.StartAutoSave(_configPath);
                    }
                });
            }

            EditorCommon.DrawSeparator();
        } else {
            GUILayout.Space(10f);
        }

        if (_config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            
            GUILayout.Label("AssetBundle 빌드 폴더", Constants.Editor.AREA_TITLE_STYLE);
            _config.buildDirectory = EditorCommon.DrawFolderSelector("선택", _config.buildDirectory, width:40f);
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label("캐싱(Caching)", Constants.Editor.AREA_TITLE_STYLE);
            _config.isActiveCaching = EditorCommon.DrawLabelToggle(_config.isActiveCaching, "Caching 활성화", 100f);
            
            if (_config.isActiveCaching && _service.IsReady()) {
                EditorCommon.DrawLabelTextField("현재 활성화된 Caching 폴더", _service.Get().path, 170f);
            }
            
            EditorCommon.DrawSeparator();

            GUILayout.Label("암호화", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.HorizontalScope("box")) {
                _config.isAssetBundleManifestEncrypted = EditorCommon.DrawLabelToggle(_config.isAssetBundleManifestEncrypted, "AssetBundleManifest 암호화 활성화", 220f);
            }

            using (new GUILayout.VerticalScope("box")) {
                using (new GUILayout.HorizontalScope()) {
                    using (new EditorGUI.DisabledGroupScope(_config.isAssetBundleSelectableEncrypted)) {
                        EditorCommon.SetGUITooltip("AssetBundle 선택적 암호화가 활성화 되어 있습니다. 둘 중 하나만 활성화가 가능합니다.", _config.isAssetBundleEncrypted);
                        _config.isAssetBundleEncrypted = EditorCommon.DrawLabelToggle(_config.isAssetBundleEncrypted, "AssetBundle 암호화 활성화", GUI.tooltip, 220f);
                    }
                    
                    using (new EditorGUI.DisabledGroupScope(_config.isAssetBundleEncrypted)) {
                        EditorCommon.SetGUITooltip("AssetBundle 암호화가 활성화 되어 있습니다. 둘 중 하나만 활성화가 가능합니다.", _config.isAssetBundleSelectableEncrypted);
                        _config.isAssetBundleSelectableEncrypted = EditorCommon.DrawLabelToggle(_config.isAssetBundleSelectableEncrypted, "AssetBundle 선택적 암호화 활성화", GUI.tooltip, 220f);
                    }
                }
                
                if (IsActiveAssetBundleEncrypt()) {
                    EditorGUILayout.HelpBox("AssetBundle 암호화 옵션은 하나만 선택이 가능합니다. 다른 옵션을 활성화 하기 위해선 현재 활성화 되어 있는 옵션을 비활성화 해야 합니다.", MessageType.Info);
                }
            }
            
            if (_config.isAssetBundleSelectableEncrypted) {
                EditorCommon.DrawSeparator();
                using (new GUILayout.VerticalScope("box")) {
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.Label("AssetBundle 선택적 암호화", Constants.Editor.AREA_TITLE_STYLE, GUILayout.ExpandWidth(false));
                        if (GUILayout.Button(REFRESH_ICON, GUILayout.Width(22f), GUILayout.Height(22f))) {
                            RefreshAssetBundleList();
                        }
                    }
                    
                    GUILayout.Space(5f);
                    
                    _selectAssetBundleScrollViewPosition = GUILayout.BeginScrollView(_selectAssetBundleScrollViewPosition, false, false, GUILayout.MinHeight(100f));
                    for (var i = 0; i < _allAssetBundleList.Count; i += 2) {
                        using (new GUILayout.HorizontalScope()) {
                            for (var j = 0; j < 2 && i + j < _allAssetBundleList.Count; j++) {
                                _config.selectAssetBundleDic[_allAssetBundleList[i + j]] = EditorCommon.DrawLabelToggle(_config.selectAssetBundleDic[_allAssetBundleList[i + j]], _allAssetBundleList[i + j], 150f);
                            }
                        }
                        GUILayout.Space(1f);
                    }
                    GUILayout.EndScrollView();
                }
            }

            using (new GUILayout.VerticalScope("box")) {
                if (IsActiveEncrypt()) {
                    if (_config.IsNull()) {
                        EditorGUILayout.HelpBox($"{nameof(Config)} 파일이 존재하지 않기 때문에 저장되지 않습니다.", MessageType.Warning);
                    }
                    
                    EditorCommon.DrawButtonPasswordField("Encrypt Key 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => _config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey), 110f);
                }
            }

            EditorCommon.DrawSeparator();

            using (new GUILayout.VerticalScope("box")) {
                GUILayout.Label("AssetBundle 빌드 옵션", Constants.Editor.AREA_TITLE_STYLE);
                _buildOptionScrollViewPosition = GUILayout.BeginScrollView(_buildOptionScrollViewPosition, false, false, GUILayout.Height(200f));
                foreach (var option in BUILD_OPTION_LIST) {
                    _config.buildOptionDic[option] = EditorCommon.DrawLabelToggle(_config.buildOptionDic[option], option.ToString());
                    GUILayout.Space(1);
                }

                GUILayout.EndScrollView();
            }
            
            EditorCommon.DrawSeparator();

            using (new GUILayout.VerticalScope("box")) {
                GUILayout.Label("빌드", Constants.Editor.AREA_TITLE_STYLE);
                using (new GUILayout.HorizontalScope()) {
                    _config.isClearAssetBundleManifest = EditorCommon.DrawLabelToggle(_config.isClearAssetBundleManifest, "Clear .manifest", "빌드 완료 후 .manifest 확장자 파일 제거", 220f);
                }
            }
            
            using (new GUILayout.HorizontalScope("box")) {
                using (new GUILayout.VerticalScope()) {
                    _selectBuildTarget = EditorCommon.DrawEnumPopup(string.Empty, _selectBuildTarget, GUILayout.Height(20f));
                    EditorCommon.DrawLabelTextField("현재 빌드 타겟", EditorUserBuildSettings.activeBuildTarget.ToString());
                }

                if (IsActiveEncrypt() && string.IsNullOrEmpty(_plainEncryptKey)) {
                    EditorGUILayout.HelpBox("암호화 옵션이 활성화 되어 있습니다. 암호화에 필요한 키를 입력하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                } else if (string.IsNullOrEmpty(_config.buildDirectory)) {
                    EditorGUILayout.HelpBox("빌드 폴더를 선택하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                } else {
                    if (GUILayout.Button("AssetBundle 빌드", GUILayout.Width(200f), GUILayout.Height(40))) {
                        var options = BuildAssetBundleOptions.None;
                        foreach (var (option, active) in _config.buildOptionDic) {
                            if (active) {
                                options |= option;
                            }
                        }

                        if (_selectBuildTarget != EditorUserBuildSettings.activeBuildTarget) {
                            EditorCommon.OpenCheckDialogue("경고", "선택된 빌드 플랫폼과 현재 에디터의 빌드 플랫폼이 다릅니다. 전환 후 빌드하시겠습니까?\n" +
                                                                 $"{EditorUserBuildSettings.selectedBuildTargetGroup} ▷ {_selectBuildTarget.GetTargetGroup()}\n" +
                                                                 $"{EditorUserBuildSettings.activeBuildTarget} ▷ {_selectBuildTarget}\n\n" +
                                                                 $"대상 디렉토리 : {_config.buildDirectory}/{_selectBuildTarget}\n\n" +
                                                                 $"활성화된 옵션\n{options.ToString()}",
                                ok: () => {
                                    EditorUserBuildSettings.SwitchActiveBuildTarget(_selectBuildTarget.GetTargetGroup(), _selectBuildTarget);
                                    BuildAssetBundle(options);
                                });
                        } else {
                            EditorCommon.OpenCheckDialogue("에셋번들 빌드", $"에셋번들 빌드를 진행합니다.\n" +
                                                                      $"{EditorUserBuildSettings.selectedBuildTargetGroup}\n{EditorUserBuildSettings.activeBuildTarget}\n\n" +
                                                                      $"대상 디렉토리 : {_config.buildDirectory}/{_selectBuildTarget}\n\n" +
                                                                      $"활성화된 옵션\n{options.ToString()}", ok: () => BuildAssetBundle(options));
                        }
                    }
                }
                
            }
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(_config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    private void RefreshAssetBundleList() {
        _allAssetBundleList = AssetDatabase.GetAllAssetBundleNames().Where(Path.HasExtension).Distinct().ToList();
        _allAssetBundleList.ForEach(name => _config.selectAssetBundleDic.TryAdd(name, false));
    }
    
    private void OnSystemWatcherEventHandler(object ob, FileSystemEventArgs args) {
        switch (args.ChangeType) {
            case WatcherChangeTypes.Created:
                if (_config.IsNull()) {
                    _config = _config.Clone<Config>();
                }
                break;
            case WatcherChangeTypes.Deleted:
                if (_config.IsNull() == false) {
                    _config = _config.Clone<NullConfig>();
                }
                break;
        }
    }
    
    private AssetBundleManifest BuildAssetBundle(BuildAssetBundleOptions options) {
        var buildPath = $"{_config.buildDirectory}/{EditorUserBuildSettings.activeBuildTarget}";
        if (ResourceGenerator.TryGenerateAssetBundle(buildPath, options, EditorUserBuildSettings.activeBuildTarget, out var manifest)) {
            if (_config.isAssetBundleManifestEncrypted) {
                var manifestPath = Path.Combine(buildPath, EditorUserBuildSettings.activeBuildTarget.ToString());
                if (SystemUtil.TryReadAllBytes(manifestPath, out var plainBytes) && EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) { {
                    File.WriteAllBytes(manifestPath, cipherBytes);
                }}
            }

            if (_config.isAssetBundleEncrypted) {
                manifest.GetAllAssetBundles().ForEach(name => EncryptAssetBundle(buildPath, name));
            } else if (_config.isAssetBundleSelectableEncrypted) {
                _config.selectAssetBundleDic.Where(pair => pair.Value).Select(pair => pair.Key).ForEach(name => EncryptAssetBundle(buildPath, name));
            }

            if (_config.isClearAssetBundleManifest) {
                foreach (var manifestPath in Directory.GetFiles(buildPath).Where(path => Path.GetExtension(path) == Constants.Extension.MANIFEST)) {
                    File.Delete(manifestPath);
                }
            }
            
            return manifest;
        }
        
        Logger.TraceError($"{nameof(manifest)} is Null. AssetBundle Build Failed.");
        return null;
    }

    private void EncryptAssetBundle(string buildPath, string name) {
        var assetBundlePath = Path.Combine(buildPath, name);
        if (SystemUtil.TryReadAllBytes(assetBundlePath, out var plainBytes)) {
            if (EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) {
                File.WriteAllBytes(assetBundlePath, cipherBytes);
            } else {
                Logger.TraceError($"{nameof(EncryptUtil.TryEncryptAESBytes)} Failed.\n{assetBundlePath}");
            }
        } else {
            Logger.TraceError($"{nameof(SystemUtil.TryReadAllBytes)} Failed.\n{assetBundlePath}");
        }
    }

    private bool IsActiveEncrypt() => _config != null && (_config.isAssetBundleManifestEncrypted || _config.isAssetBundleEncrypted || _config.isAssetBundleSelectableEncrypted);
    private bool IsActiveAssetBundleEncrypt() => _config != null && (_config.isAssetBundleEncrypted || _config.isAssetBundleSelectableEncrypted);

    private class NullConfig : Config { }

    private class Config : JsonAutoConfig {

        public string buildDirectory = "";
        public bool isActiveCaching;
        public bool isClearAssetBundleManifest;
        
        public bool isAssetBundleManifestEncrypted;
        public bool isAssetBundleEncrypted;
        public bool isAssetBundleSelectableEncrypted;
        public readonly Dictionary<string, bool> selectAssetBundleDic = new();
        public string cipherEncryptKey = "";
        
        public readonly Dictionary<BuildAssetBundleOptions, bool> buildOptionDic = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);

        public override bool IsNull() => this is NullConfig;
    }
}