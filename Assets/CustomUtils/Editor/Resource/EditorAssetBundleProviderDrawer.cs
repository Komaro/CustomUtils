using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Codice.CM.SEIDInfo;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceProviderDrawer(typeof(AssetBundleProvider))]
public class EditorAssetBundleProviderDrawer : EditorResourceProviderDrawer {
    
    private CachingService _service;
    
    private Config _config;
    private static string _configPath;

    private static string _buildDirectory;
    private static bool _isActiveCaching;
    private static string _downloadDirectory;
    private static bool _isActiveEncrypt;
    private static string _plainEncryptKey;
    private Dictionary<BuildAssetBundleOptions, bool> _buildOptionDic = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);

    private Vector2 _buildOptionScrollViewPosition;
    
    private static string _url; // Download Test Server URL

    private readonly List<BuildAssetBundleOptions> BUILD_OPTION_LIST = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToList();
    private const string CONFIG_NAME = "AssetBundleProviderConfig.json";
    
    private readonly Regex NAME_REGEX = new(@"^[^\\/:*?""<>|]+$");

    public EditorAssetBundleProviderDrawer() => CacheRefresh();

    public sealed override void CacheRefresh() {
        _service = Service.GetService<CachingService>();
        
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }
        
        _config = null;
        if (File.Exists(_configPath) && JsonUtil.TryLoadJson(_configPath, out _config)) {
            _buildDirectory = _config.buildDirectory;
            _isActiveCaching = _config.isActiveCaching;
            _downloadDirectory = _config.downloadDirectory;
            _url = _config.downloadUrl;
            
            _isActiveEncrypt = _config.isActiveEncrypt;
            _plainEncryptKey = _isActiveEncrypt && string.IsNullOrEmpty(_config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(_config.cipherEncryptKey) : string.Empty;

            _config.buildOptionDic.ForEach(x => _buildOptionDic.AutoAdd(x.Key, x.Value));
        }
    }

    public override void Draw() {
        if (_config == null) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 AssetBundle 설정이 저장되지 않으며 일부 기능을 사용할 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button($"{nameof(Config)} 파일 생성")) {
                EditorCommon.ShowCheckDialogue($"{nameof(Config)} 파일 생성", $"{nameof(Config)} 파일을 생성합니다.\n경로는 아래와 같습니다.\n{_configPath}", ok: () => {
                    _config = new Config();
                    _config.Save(_configPath);
                });
            }
            
            EditorCommon.DrawSeparator();
        }
        
        _buildDirectory = EditorCommon.DrawFolderSelector("빌드 폴더 선택", _buildDirectory, selectDirectory => {
            if (_config != null) {
                _config.buildDirectory = selectDirectory;
                _config?.Save(_configPath);
            }
        });
        
        EditorCommon.DrawSeparator();
        
        _isActiveCaching = EditorCommon.DrawLabelToggle(_isActiveCaching, "Caching 활성화", 100f);
        if (_config != null && _config.isActiveCaching != _isActiveCaching) {
            _config.isActiveCaching = _isActiveCaching;
            _config.Save(_configPath);
        }

        if (_isActiveCaching && _service.IsReady()) {
            EditorCommon.DrawLabelTextSet("현재 활성화된 Caching 폴더", _service.Get().path, 170f);
        } else {
            _downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", _downloadDirectory, selectDirectory => {
                if (_config != null) {
                    _config.downloadDirectory = selectDirectory;
                    _config.Save(_configPath);
                }
            });
        }
        
        GUILayout.Space(5);
        _isActiveEncrypt = EditorCommon.DrawLabelToggle(_isActiveEncrypt, "암호화 활성화", 100f);
        if (_config != null && _config.isActiveEncrypt != _isActiveEncrypt) {
            _config.isActiveEncrypt = _isActiveEncrypt;
            _config.Save(_configPath);
        }

        if (_isActiveEncrypt) {
            _plainEncryptKey = EditorCommon.DrawButtonPasswordFieldSet("저장", _plainEncryptKey, plainEncryptKey => {
                if (_config != null) {
                    _config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey);
                    _config.Save(_configPath);
                }
            }, 60f);
        }
        
        EditorCommon.DrawSeparator();

        
        using (new GUILayout.VerticalScope("box")) {
            GUILayout.Label("AssetBundle 빌드 옵션 선택", Constants.Editor.FIELD_TITLE_STYLE);
            _buildOptionScrollViewPosition = GUILayout.BeginScrollView(_buildOptionScrollViewPosition, false, false, GUILayout.Height(250f));
            foreach (var option in BUILD_OPTION_LIST) {
                _buildOptionDic[option] = EditorCommon.DrawLabelToggle(_buildOptionDic[option], option.ToString());
                if (_config != null && _config.buildOptionDic.TryGetValue(option, out var active) && _buildOptionDic[option] != active) {
                    _config.buildOptionDic[option] = _buildOptionDic[option];
                    _config.Save(_configPath);
                }

                GUILayout.Space(1);
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("AssetBundle 빌드", GUILayout.Height(40))) {
                // TODO. 빌드 옵션 조합
                var options = BuildAssetBundleOptions.None;
                foreach (var (option, active) in _buildOptionDic) {
                    if (active) {
                        options |= option;
                    }
                }
                
                // TODO. 현재 타겟 플랫폼과 빌드 할 타겟 플랫폼을 체크 후 변경할 지 Show Dialogue 처리 추가 
                // TODO. enum 값들을 가져올 때 Obsolete 체크 후 캐싱 처리 
                var buildSetting = BuildTarget.NoTarget;
                
                // TODO. 최종 에셋번들 빌드 처리
                // Sample
                ResourceGenerator.GenerateAssetBundle(_buildDirectory, options, EditorUserBuildSettings.activeBuildTarget);
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
    }

    // Sample
    private void BuildAssetBundle() {
        ResourceGenerator.GenerateAssetBundle($"C:/Project/Unity/CustomUtils/AssetBundle/{EditorUserBuildSettings.activeBuildTarget}", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
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
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAES(request.downloadHandler.data));
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
                    var decryptBytes = EncryptUtil.DecryptAES(request.downloadHandler.data);
                    assetBundle = AssetBundle.LoadFromMemory(decryptBytes);
                } finally {
                    if (assetBundle != null) {
                        callback?.Invoke(assetBundle);
                    }
                }
            }
        };
    }

    private class Config : JsonConfig {
        
        public string buildDirectory = "";

        public bool isActiveCaching;
        public string downloadDirectory = "";

        public string downloadUrl = "";
        
        public bool isActiveEncrypt;
        public string cipherEncryptKey = "";

        public Dictionary<BuildAssetBundleOptions, bool> buildOptionDic = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);
    }
}