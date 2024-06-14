using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceTesterDrawer(typeof(AssetBundleProvider))]
public class EditorAssetBundleTesterDrawer : EditorResourceDrawer {

    private Config _config;
    private string _configPath;
    
    private string _downloadDirectory; 
    private string _url;
    private string _encryptKey;

    private const string CONFIG_NAME = "AssetBundleTesterConfig.json";
    private readonly string CONFIG_PATH = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";

    public override void CacheRefresh() {

        
        
    }

    public override void Draw() {
        _downloadDirectory = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", _downloadDirectory);
        _url = EditorCommon.DrawLabelTextField("URL", _url);
        
        // TODO. 암호화 키 입력
        _encryptKey = EditorCommon.DrawLabelTextField("암호화 키", _encryptKey);
        
        // TODO. 캐싱 기능 온오프 처리
        
        // TODO. 매니페스트 다운로드를 통한 전체 에셋번들 처리 확인
        
        if (GUILayout.Button("AssetBundle Download Test")) {
            AssetBundle.UnloadAllAssetBundles(true);
            AssetBundleManifestDownload($"{EditorUserBuildSettings.activeBuildTarget}/{EditorUserBuildSettings.activeBuildTarget}", manifest => {
                foreach (var assetBundleName in manifest.GetAllAssetBundles()) {
                    var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                    AssetBundleDownload(assetBundlePath, manifest.GetAssetBundleHash(assetBundleName), assetBundle => {
                        Logger.TraceError($"{assetBundle.name} || {manifest.GetAssetBundleHash(assetBundle.name)}");
                    });
                }
            });
        }
    }
    
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

                try {
                    var assetBundle = AssetBundle.LoadFromMemory(request.downloadHandler.data);
                    if (assetBundle == null) {
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
                    Logger.TraceError($"{nameof(AssetBundle)} Resource download was successful, but memory load failed. It appears to be either an incorrect format or a decryption failure. The issue might be related to the encryption key.\n\n{ex}");
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
                try {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (assetBundle == null) {
                        assetBundle = AssetBundle.LoadFromMemory(EncryptUtil.DecryptAESBytes(request.downloadHandler.data));
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

    private class NullConfig : Config { }
    
    private class Config : JsonAutoConfig {

        public string downloadDirectory; 
        public string url;
        public string encryptKey;
        
        public override bool IsNull() => this is NullConfig;
    }
}
