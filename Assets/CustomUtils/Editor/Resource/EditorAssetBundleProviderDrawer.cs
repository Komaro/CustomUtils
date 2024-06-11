using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceProviderDrawer(typeof(AssetBundleProvider))]
public class EditorAssetBundleProviderDrawer : EditorResourceProviderDrawer {
    
    private CachingService _service;
    
    private Config _config;
    private static string _configPath;

    private bool _isShowEncryptKey = false;
    private BuildTarget _selectBuildTarget = EditorUserBuildSettings.activeBuildTarget;
    
    private static string _plainEncryptKey;
    private static string _downloadDirectory; 
    private static string _url; // Download Test Server URL
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _selectAssetBundleScrollViewPosition;
    private Vector2 _buildOptionScrollViewPosition;

    private readonly List<BuildAssetBundleOptions> BUILD_OPTION_LIST;
    
    private const string CONFIG_NAME = "AssetBundleProviderConfig.json";
    
    private readonly Regex NAME_REGEX = new(@"^[^\\/:*?""<>|]+$");
    
    public EditorAssetBundleProviderDrawer() {
        BUILD_OPTION_LIST = EnumUtil.GetValues<BuildAssetBundleOptions>(true, true).ToList();
        CacheRefresh();
        
        // TODO. 중복 방지 및 간소화 처리 필요
        // Service.GetService<SystemWatcherService>().AddWatcher(new SystemWatcherServiceOrder {
        //     path = Constants.Editor.COMMON_CONFIG_FOLDER, filter = CONFIG_NAME, filters = NotifyFilters.LastWrite | NotifyFilters.FileName,
        //     handler = (_, args) => {
        //         switch (args.ChangeType) {
        //             case WatcherChangeTypes.Created:
        //                 if (_config.IsNull()) {
        //                     _config = _config.Clone<Config>();
        //                 }
        //                 break;
        //             case WatcherChangeTypes.Deleted:
        //                 if (_config.IsNull() == false) {
        //                     _config = _config.Clone<NullConfig>();
        //                 }
        //                 break;
        //         }
        //     }
        // });
    }

    public override void Close() {
        if (_config != null && _config.IsNull()) {
            _config.StopAutoSave();
            _config.Save(_configPath);
        }
        
        // Service.GetService<SystemWatcherService>().StopWatcher(Constants.Editor.COMMON_CONFIG_FOLDER);
    }

    public sealed override void CacheRefresh() {
        _service = Service.GetService<CachingService>();
        // Service.GetService<SystemWatcherService>().StartWatcher(Constants.Editor.COMMON_CONFIG_FOLDER);
        
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }
        
        if (File.Exists(_configPath) && JsonUtil.TryLoadJson(_configPath, out _config)) {
            _plainEncryptKey = string.IsNullOrEmpty(_config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(_config.cipherEncryptKey) : string.Empty;
            _downloadDirectory = _config.downloadDirectory;
            _url = _config.downloadUrl;
            
            _config.StartAutoSave(_configPath);
        } else {
            if (_config == null || _config.IsNull() == false) {
                _config = new NullConfig();
            }
        }
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
            } else {
                _config.downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", _config.downloadDirectory);
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
                
                // TODO. 에셋번들 개별 선택 처리 수정
                if (_config.isAssetBundleSelectableEncrypted) {
                    EditorCommon.DrawSeparator();
                    var assetBundleList = AssetDatabase.GetAllAssetBundleNames().Where(Path.HasExtension);
                    foreach (var name in assetBundleList) {
                        if (_config.selectAssetBundleDic.ContainsKey(name)) {
                            _config.selectAssetBundleDic[name] = EditorCommon.DrawLabelToggle(_config.selectAssetBundleDic[name], name);
                        } else {
                            _config.selectAssetBundleDic.AutoAdd(name, false);
                        }
                            
                        GUILayout.Space(1f);
                    }
                }

                if (IsActiveAssetBundleEncrypt()) {
                    EditorGUILayout.HelpBox("AssetBundle 암호화 옵션은 하나만 선택이 가능합니다. 다른 옵션을 활성화 하기 위해선 현재 활성화 되어 있는 옵션을 비활성화 해야 합니다.", MessageType.Info);
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
                    _config.isClearAssetBundleManifest = EditorCommon.DrawLabelToggle(_config.isClearAssetBundleManifest, "Manifest Clear", "빌드 완료 후 .manifest 확장자 파일 제거", 220f);
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
            

            if (GUILayout.Button("Request Test")) {
                AssetBundle.UnloadAllAssetBundles(true);
                AssetBundleManifestDownload($"{EditorUserBuildSettings.activeBuildTarget}/{EditorUserBuildSettings.activeBuildTarget}", manifest => {
                    foreach (var assetBundleName in manifest.GetAllAssetBundles()) {
                        var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                        AssetBundleDownload(assetBundlePath, manifest.GetAssetBundleHash(assetBundleName), assetBundle => {
                            // Logger.TraceError($"{assetBundle.name} || {manifest.GetAssetBundleHash(assetBundle.name)}");
                        });
                    }
                });
            }
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(_config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    // Sample
    private AssetBundleManifest BuildAssetBundle(BuildAssetBundleOptions options) {
        var buildPath = $"{_config.buildDirectory}/{EditorUserBuildSettings.activeBuildTarget}";
        if (ResourceGenerator.TryGenerateAssetBundle(buildPath, options, EditorUserBuildSettings.activeBuildTarget, out var manifest)) {
            if (_config.isAssetBundleManifestEncrypted) {
                var manifestPath = Path.Combine(buildPath, EditorUserBuildSettings.activeBuildTarget.ToString());
                if (SystemUtil.TryReadAllBytes(manifestPath, out var plainBytes) && EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) { {
                    File.WriteAllBytes(manifestPath, cipherBytes);
                }}
            }

            // TODO. 중복 처리 존재. 메소드 추출 필요
            if (_config.isAssetBundleEncrypted) {
                foreach (var name in manifest.GetAllAssetBundles()) {
                    var assetBundlePath = Path.Combine(buildPath, name);
                    if (SystemUtil.TryReadAllBytes(assetBundlePath, out var plainBytes) && EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) {
                        File.WriteAllBytes(assetBundlePath, cipherBytes);
                    }
                }
            } else if (_config.isAssetBundleSelectableEncrypted) {
                foreach (var name in _config.selectAssetBundleDic.Where(pair => pair.Value).Select(pair => pair.Key)) {
                    var assetBundlePath = Path.Combine(buildPath, name);
                    if (SystemUtil.TryReadAllBytes(assetBundlePath, out var plainBytes) && EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) {
                        File.WriteAllBytes(assetBundlePath, cipherBytes);
                    }
                }
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

    // Caching 안됨
    private void AssetBundleManifestDownload(string name, Action<AssetBundleManifest> callback = null) {
        var request = UnityWebRequest.Get(Path.Combine(_url, name));
        Logger.TraceLog($"Request || {request.url}", Color.cyan);
        request.SendWebRequest().completed += _ => {
            if (request.result != UnityWebRequest.Result.Success) {
                Logger.TraceErrorExpensive(request.error);
            } else {
                if (request.responseCode != (long) HttpStatusCode.OK) {
                    Logger.TraceError($"Already ResponseCode || {request.responseCode}");
                    return;
                }

                AssetBundle assetBundle;
                try {
                    try {
                        assetBundle = AssetBundle.LoadFromMemory(request.downloadHandler.data);
                    } catch (Exception ex) {
                        Logger.TraceError(ex);
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data));
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
                    Logger.TraceError($"{nameof(assetBundle)} is Null. Resource download was successful, but memory load failed. It appears to be either an incorrect format or a decryption failure. The issue might be related to the encryption key.\n\n{ex}");
                }
            }
        };
    }
    
    // Caching 됨
    private void AssetBundleDownload(string name, Hash128 hash, Action<AssetBundle> callback = null) {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(_url, name), hash);
        Logger.TraceLog($"Request || {request.url}", Color.cyan);
        request.SendWebRequest().completed += operation => {
            if (request.result != UnityWebRequest.Result.Success) {
                Logger.TraceError(request.error);
            } else {
                AssetBundle assetBundle = null;
                try {
                    assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                } catch (Exception ex) {
                    Logger.TraceLog(ex, Color.red);
                    var decryptBytes = EncryptUtil.DecryptAESBytes(request.downloadHandler.data);
                    assetBundle = AssetBundle.LoadFromMemory(decryptBytes);
                } finally {
                    if (assetBundle != null) {
                        callback?.Invoke(assetBundle);
                    }
                }
            }
        };
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
        public Dictionary<string, bool> selectAssetBundleDic = new();
        public string cipherEncryptKey = "";
        
        public string downloadDirectory = "";
        public string downloadUrl = "";

        public readonly Dictionary<BuildAssetBundleOptions, bool> buildOptionDic = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);

        public override bool IsNull() => this is NullConfig;
    }
}