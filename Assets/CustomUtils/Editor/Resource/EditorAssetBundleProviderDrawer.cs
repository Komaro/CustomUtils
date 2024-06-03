using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceProviderDrawer(typeof(AssetBundleProvider))]
public class EditorAssetBundleProviderDrawer : EditorResourceProviderDrawer {

    private Config _config;
    
    private static string _configPath;


    private static string _buildDirectory;

    private static bool _isActiveCaching;
    private static string _cachingDirectoryName = "AssetBundles";
    
    private static string _downloadDirectory;
    
    private static string _url; // Download Test Server URL
    
    private const string CONFIG_NAME = "AssetBundleProviderConfig.json";

    private readonly Regex NAME_REGEX = new(@"^[^\\/:*?""<>|]+$");

    public EditorAssetBundleProviderDrawer() => CacheRefresh();

    public sealed override void CacheRefresh() {
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }
        
        _config = null;
        if (File.Exists(_configPath) && JsonUtil.TryLoadJson(_configPath, out _config)) {
            _buildDirectory = _config.buildDirectory;
            _isActiveCaching = _config.isActiveCaching;
            _cachingDirectoryName = _config.cachingDirectoryName;
            _downloadDirectory = _config.downloadDirectory;
            _url = _config.downloadUrl;
        }
        
        if (_isActiveCaching) {
            SetCaching(_cachingDirectoryName);
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

        if (_config != null) {
            _isActiveCaching = GUILayout.Toggle(_isActiveCaching, "Caching 활성화");
            if (_config.isActiveCaching != _isActiveCaching) {
                _config.isActiveCaching = _isActiveCaching;
                _config.Save(_configPath);
            }

            if (_config.isActiveCaching && Caching.ready) {
                _cachingDirectoryName = EditorCommon.DrawInputFieldSet("Caching 폴더명", _cachingDirectoryName);
                if (NAME_REGEX.IsMatch(_cachingDirectoryName) == false) {
                    EditorGUILayout.HelpBox("유효하지 않은 폴더명입니다.", MessageType.Warning);
                } else {
                    if (GUILayout.Button("Caching 폴더명 저장") && string.IsNullOrEmpty(_cachingDirectoryName) == false) {
                        SetCaching(_cachingDirectoryName);
                        _config.cachingDirectoryName = _cachingDirectoryName;
                        _config.Save(_configPath);
                    }
                }
                
                if (Caching.ready) {
                    EditorCommon.DrawLabelTextSet("활성화된 Caching 폴더", Caching.currentCacheForWriting.path, 150);
                    if (GUILayout.Button("Caching 클리어")) {
                        AssetBundle.UnloadAllAssetBundles(false);
                        Logger.TraceLog(Caching.ClearCache() ? $"Cache Clear Success || {Caching.currentCacheForWriting.path}" : "Cache Clear Failed", Color.yellow);
                    }

                    if (GUILayout.Button("Caching 폴더")) {
                        EditorUtility.RevealInFinder(Caching.currentCacheForWriting.path);
                    }
                }
            } else {
                _downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", _downloadDirectory, selectDirectory => {
                    _config.downloadDirectory = selectDirectory;
                    _config.Save(_configPath);
                });
            }
        }
        
        // TODO. Draw Encrypt Option

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
        if (_config != null && Caching.ready) {
            var cachePath = $"{Application.persistentDataPath}/{directoryName}";
            var cache = Caching.GetCacheByPath(cachePath);
            if (cache.valid == false) {
                // TODO. 캐시 폴더가 없는 경우 생성하는 처리

                // TODO. 인게임 Caching 처리를 위한 CachingService.cs 개발 필요
                cache = Caching.AddCache(cachePath);
            }
                
            Caching.currentCacheForWriting = cache;
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
        public string cachingDirectoryName = "AssetBundles";
        public string downloadDirectory = "";

        public string downloadUrl = "";
    }
}