using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class EditorAssetBundleProviderDrawer {
    
    private void DrawBuildButton() {
        if (GUILayout.Button("AssetBundle 빌드", GUILayout.Width(200f), GUILayout.Height(40))) {
            var options = BuildAssetBundleOptions.None;
            foreach (var (option, active) in config.buildOptionDic) {
                if (active) {
                    options |= option;
                }
            }

            if (config.selectBuildTarget != EditorUserBuildSettings.activeBuildTarget) {
                EditorCommon.ShowCheckDialogue("경고", "선택된 빌드 플랫폼과 현재 에디터의 빌드 플랫폼이 다릅니다. 전환 후 빌드하시겠습니까?\n" +
                                                     $"{EditorUserBuildSettings.selectedBuildTargetGroup} ▷ {config.selectBuildTarget.GetTargetGroup()}\n" +
                                                     $"{EditorUserBuildSettings.activeBuildTarget} ▷ {config.selectBuildTarget}\n\n" +
                                                     $"대상 디렉토리 : {config.GetBuildPath()}\n\n" +
                                                     $"활성화된 옵션\n{options.ToString()}",
                    ok: () => {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(config.selectBuildTarget.GetTargetGroup(), config.selectBuildTarget);
                        BuildAssetBundleWithLogging(options);
                    });
            } else {
                EditorCommon.ShowCheckDialogue("에셋번들 빌드", $"에셋번들 빌드를 진행합니다.\n" +
                                                          $"{EditorUserBuildSettings.selectedBuildTargetGroup}\n{EditorUserBuildSettings.activeBuildTarget}\n\n" +
                                                          $"대상 디렉토리 : {config.buildDirectory}/{config.selectBuildTarget}\n\n" +
                                                          $"활성화된 옵션\n{options.ToString()}", ok: () => BuildAssetBundleWithLogging(options));
            }
        }
    }

    private AssetBundleManifest BuildAssetBundleWithLogging(BuildAssetBundleOptions options) {
        var info = new AssetBundleBuildInfo {
            buildStartTime = DateTime.Now,
            buildPath = config.GetBuildPath()
        };

        if (Service.TryGetServiceWithRestart<LogCollectorService>(out var service)) {
            service.ClearLog();
            service.SetFilter(LogType.Error);
        }
        
        var manifest = BuildAssetBundle(options);
        info.buildSuccess = manifest != null;
        info.buildTarget = config.selectBuildTarget;
        info.buildEndTime = DateTime.Now;
        info.memo = config.isLogBuildSetting == false ? _buildInfoMemo : $"{_buildInfoMemo}\n\n===================\n\n" +
                                                                         $"{config.ToStringAllFields()}\n\n===================\n\n" +
                                                                         $"{service?.Copy().ToStringCollection("\n")}";
        
        Service.StopService<LogCollectorService>();
        
        config.AddBuildInfo(info);
        config.Save(CONFIG_PATH);

        _buildInfoCursor = config.GetInfoCount() - 1;
        
        return manifest;
    }

    private AssetBundleManifest BuildAssetBundle(BuildAssetBundleOptions options) {
        var buildPath = config.GetBuildPath();
        var manifest = config.isSelectableBuild
            ? ResourceGenerator.GenerateAssetBundle(buildPath, options, config.selectBuildTarget, config.assetBundleInfoDic.Values.Where(x => x.isSelect).Select(x => x.name).ToArray()) 
            : ResourceGenerator.GenerateAssetBundle(buildPath, options, config.selectBuildTarget);
        
        if (manifest != null) {
            if (config.isGenerateChecksumInfo) {
                var checksumInfoPath = $"{buildPath}/{(string.IsNullOrEmpty(config.checksumFileName) ? nameof(AssetBundleChecksumInfo) : config.checksumFileName)}";
                var info = GenerateChecksumInfo(manifest, buildPath);
                if (config.isEncryptChecksum) {
                    JsonUtil.SaveEncryptJson(checksumInfoPath, info, _plainEncryptKey);
                } else {
                    JsonUtil.SaveJson(checksumInfoPath, info);
                }
            }
            
            if (config.isAssetBundleManifestEncrypted) {
                var manifestPath = Path.Combine(buildPath, config.selectBuildTarget.ToString());
                if (SystemUtil.TryReadAllBytes(manifestPath, out var plainBytes) && EncryptUtil.TryEncryptAESBytes(out var cipherBytes, plainBytes, _plainEncryptKey)) { {
                    File.WriteAllBytes(manifestPath, cipherBytes);
                }}
            }

            if (config.isAssetBundleEncrypted) {
                manifest.GetAllAssetBundles().ForEach(name => EncryptAssetBundle(buildPath, name));
            } else if (config.isAssetBundleSelectableEncrypted) {
                config.assetBundleInfoDic.Values.Where(x => x.isEncrypt).Select(x => x.name).ForEach(name => EncryptAssetBundle(buildPath, name));
            }

            // The process must be handled only after obtaining CRC information.
            if (config.isClearAssetBundleManifest) {
                foreach (var manifestPath in Directory.GetFiles(buildPath).Where(path => Path.GetExtension(path) == Constants.Extension.MANIFEST)) {
                    File.Delete(manifestPath);
                }
            }
            
            return manifest;
        }
        
        Logger.TraceError($"{nameof(manifest)} is Null. AssetBundle Build Failed.");
        return null;
    }
}