using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[EditorResourceTesterDrawer(typeof(EditorAssetBundleTesterDrawer))]
public class EditorAssetBundleTesterDrawer : EditorResourceDrawer {
    
    private static string _downloadDirectory; 
    private static string _url;

    public override void CacheRefresh() {

    }

    public override void Draw() {
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
        
        var temp = EditorCommon.DrawFolderSelector("다운로드 폴더 선택", string.Empty);
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
}
