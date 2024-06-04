using System;
using System.IO;
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

    private static string _buildDirectory;
    private static bool _isActiveCaching;
    private static string _downloadDirectory;
    private static bool _isActiveEncrypt;
    private static string _plainEncryptKey;
    
    private static string _url; // Download Test Server URL
    
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

        _isActiveCaching = GUILayout.Toggle(_isActiveCaching, "Caching 활성화");
        if (_config != null) {
            _config.isActiveCaching = _isActiveCaching;
            _config.Save(_configPath);
        }

        if (_isActiveCaching && _service.IsReady()) {
            EditorCommon.DrawLabelTextSet("현재 활성화된 Caching 폴더", _service.Get().path, 170);
        } else {
            _downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", _downloadDirectory, selectDirectory => {
                if (_config != null) {
                    _config.downloadDirectory = selectDirectory;
                    _config.Save(_configPath);
                }
            });
        }
        
        GUILayout.Space(5);
        _isActiveEncrypt = GUILayout.Toggle(_isActiveEncrypt, "암호화 활성화");
        if (_config != null) {
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
        
        // TODO. Build AssetBundle
        
        // TODO. Draw AssetBundle Build Button
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

    private void SetCaching(string directoryName) {
        if (_config != null && _service.IsReady()) {
            _service.Set(directoryName);
        }
    }
    
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

                var bytes = request.downloadHandler.data;
                AssetBundle assetBundle = null;
                try {
                    try {
                        assetBundle = AssetBundle.LoadFromMemory(bytes);
                    } catch (Exception e) {
                        Logger.TraceError(e);
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAES(bytes));
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
                var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                if (assetBundle != null) {
                    callback?.Invoke(assetBundle);
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
    }
}