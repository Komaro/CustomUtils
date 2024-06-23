using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Provider, typeof(AssetBundleProvider))]
public class EditorAssetBundleProviderDrawer : EditorResourceDrawerAutoConfig<AssetBundleProviderConfig, AssetBundleProviderConfig.NullConfig> {
    
    private List<string> _allAssetBundleList = new();
    
    private int _buildInfoCursor;
    private string _buildInfoMemo;
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;
    private Vector2 _windowScrollViewPosition;
    private Vector2 _selectAssetBundleScrollViewPosition;
    private Vector2 _buildOptionScrollViewPosition;
    private Vector2 _buildInfoMemoScrollViewPosition;
    
    private readonly List<BuildAssetBundleOptions> BUILD_OPTION_LIST;
    private readonly Regex NAME_REGEX = new(@"^[^\\/:*?""<>|]+$");
    private readonly GUIContent REFRESH_ICON = new (string.Empty, EditorGUIUtility.IconContent("d_Refresh").image);

    protected override string CONFIG_NAME => $"{nameof(AssetBundleProviderConfig)}{Constants.Extension.JSON}";
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";

    public EditorAssetBundleProviderDrawer() => BUILD_OPTION_LIST = EnumUtil.GetValues<BuildAssetBundleOptions>(true, true).ToList();

    public sealed override void CacheRefresh() {
        Service.GetService<SystemWatcherService>().Start(watcherOrder);
        
        if (JsonUtil.TryLoadJson(CONFIG_PATH, out config)) {
            _plainEncryptKey = string.IsNullOrEmpty(config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(config.cipherEncryptKey) : string.Empty;
            config.StartAutoSave(CONFIG_PATH);
        } else {
            if (config == null || config.IsNull() == false) {
                config = new AssetBundleProviderConfig.NullConfig();
            }
        }

        RefreshAssetBundleList();
        _buildInfoCursor = config.GetInfoCount() - 1;
        BUILD_OPTION_LIST.ForEach(option => config.buildOptionDic.TryAdd(option, false));
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            
            GUILayout.Label("AssetBundle 빌드 폴더", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                EditorCommon.DrawFolderOpenSelector("빌드 폴더", "선택", ref config.buildDirectory, width:60f);
            }
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label("보안", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isAssetBundleManifestEncrypted, "AssetBundleManifest 암호화 활성화", 210f);
                    GUILayout.FlexibleSpace();
                }
                
                using (new GUILayout.HorizontalScope()) {
                    using (new EditorGUI.DisabledGroupScope(config.isAssetBundleSelectableEncrypted)) {
                        EditorCommon.DrawLabelToggle(ref config.isAssetBundleEncrypted, "AssetBundle 암호화 활성화", "AssetBundle 선택적 암호화가 활성화 되어 있습니다. 둘 중 하나만 활성화가 가능합니다.", 210f);
                    }
                    
                    using (new EditorGUI.DisabledGroupScope(config.isAssetBundleEncrypted)) {
                        EditorCommon.DrawLabelToggle(ref config.isAssetBundleSelectableEncrypted, "AssetBundle 선택적 암호화 활성화", "AssetBundle 암호화가 활성화 되어 있습니다. 둘 중 하나만 활성화가 가능합니다.", 210f);
                    }
                    
                    GUILayout.FlexibleSpace();
                }
            }
            
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isGenerateChecksumInfo, "Checksum 정보 생성", $"빌드 완료 후 각 전체 Checksum 정보를 기록한 파일 생성", 210f);
                    if (config.isGenerateChecksumInfo) {
                        EditorCommon.DrawLabelToggle(ref config.isEncryptChecksum, "Checksum 정보 암호화", "빌드 완료 후 생성된 Checksum 파일을 암호화", 210f);
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                
                if (config.isGenerateChecksumInfo) {
                    EditorCommon.DrawLabelTextField("Checksum 정보 파일명", ref config.checksumFileName, 150f);
                }
            }

            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                if (config.IsActiveAssetBundleEncrypt()) {
                    EditorGUILayout.HelpBox("AssetBundle 암호화 옵션은 하나만 선택이 가능합니다. 다른 옵션을 활성화 하기 위해선 현재 활성화 되어 있는 옵션을 비활성화 해야 합니다.", MessageType.Info);
                }

                if (config.IsActiveEncrypt()) {
                    EditorGUILayout.HelpBox($"암호화가 활성화 되었습니다. {nameof(Caching)} 기능을 사용할 수 없습니다.", MessageType.Warning);
                }
            }
            
            if (config.isAssetBundleSelectableEncrypted) {
                EditorCommon.DrawSeparator();
                using (new GUILayout.HorizontalScope()) {
                    GUILayout.Label("AssetBundle 선택적 암호화", Constants.Editor.AREA_TITLE_STYLE, GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(REFRESH_ICON, GUILayout.Width(22f), GUILayout.Height(22f))) {
                        RefreshAssetBundleList();
                    }
                }
                
                using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                    GUILayout.Space(5f);
                    _selectAssetBundleScrollViewPosition = GUILayout.BeginScrollView(_selectAssetBundleScrollViewPosition, false, false, GUILayout.MinHeight(100f));
                    for (var i = 0; i < _allAssetBundleList.Count; i += 2) {
                        using (new GUILayout.HorizontalScope()) {
                            for (var j = 0; j < 2 && i + j < _allAssetBundleList.Count; j++) {
                                config.selectAssetBundleDic[_allAssetBundleList[i + j]] = EditorCommon.DrawLabelToggle(config.selectAssetBundleDic[_allAssetBundleList[i + j]], _allAssetBundleList[i + j], 150f);
                            }
                        }
                        GUILayout.Space(1f);
                    }
                    GUILayout.EndScrollView();
                }
            }

            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                if (config.IsActiveEncrypt()) {
                    if (config.IsNull()) {
                        EditorGUILayout.HelpBox($"{nameof(AssetBundleProviderConfig)} 파일이 존재하지 않기 때문에 저장되지 않습니다.", MessageType.Warning);
                    }
                    
                    EditorCommon.DrawButtonPasswordField("Encrypt Key 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => config.cipherEncryptKey = EncryptUtil.EncryptAES(plainEncryptKey), 110f);
                }
            }

            EditorCommon.DrawSeparator();

            GUILayout.Label("AssetBundle 빌드 옵션", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                _buildOptionScrollViewPosition = GUILayout.BeginScrollView(_buildOptionScrollViewPosition, false, false, GUILayout.Height(200f));
                foreach (var option in BUILD_OPTION_LIST) {
                    config.buildOptionDic[option] = EditorCommon.DrawLabelToggle(config.buildOptionDic[option], option.ToString());
                    GUILayout.Space(1);
                }

                GUILayout.EndScrollView();
            }
            
            EditorCommon.DrawSeparator();

            GUILayout.Label("빌드", Constants.Editor.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isClearAssetBundleManifest, ".manifest 제거", "빌드 완료 후 .manifest 확장자 파일 제거", 180f);
                    EditorCommon.DrawLabelToggle(ref config.isLogBuildSetting, "빌드 세팅 기록", $"빌드 완료 후 {nameof(AssetBundleProviderConfig)}를 메모에 기록", 180f);
                    GUILayout.FlexibleSpace();
                }
            }
            
            using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    using (new GUILayout.VerticalScope()) {
                        EditorCommon.DrawEnumPopup(string.Empty, ref config.selectBuildTarget, 0, GUILayout.Height(20f));
                        EditorCommon.DrawLabelTextField("현재 빌드 타겟", EditorUserBuildSettings.activeBuildTarget.ToString());
                    }

                    if (config.IsActiveEncrypt() && string.IsNullOrEmpty(_plainEncryptKey) == false) {
                        EditorGUILayout.HelpBox("암호화 옵션이 활성화 되어 있습니다. 암호화에 필요한 키를 입력하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                    } else if (string.IsNullOrEmpty(config.buildDirectory)) {
                        EditorGUILayout.HelpBox("빌드 폴더를 선택하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                    } else {
                        if (GUILayout.Button("AssetBundle 빌드", GUILayout.Width(200f), GUILayout.Height(40))) {
                            var options = BuildAssetBundleOptions.None;
                            foreach (var (option, active) in config.buildOptionDic) {
                                if (active) {
                                    options |= option;
                                }
                            }

                            if (config.selectBuildTarget != EditorUserBuildSettings.activeBuildTarget) {
                                EditorCommon.OpenCheckDialogue("경고", "선택된 빌드 플랫폼과 현재 에디터의 빌드 플랫폼이 다릅니다. 전환 후 빌드하시겠습니까?\n" +
                                                                     $"{EditorUserBuildSettings.selectedBuildTargetGroup} ▷ {config.selectBuildTarget.GetTargetGroup()}\n" +
                                                                     $"{EditorUserBuildSettings.activeBuildTarget} ▷ {config.selectBuildTarget}\n\n" +
                                                                     $"대상 디렉토리 : {config.GetBuildPath()}\n\n" +
                                                                     $"활성화된 옵션\n{options.ToString()}",
                                    ok: () => {
                                        EditorUserBuildSettings.SwitchActiveBuildTarget(config.selectBuildTarget.GetTargetGroup(), config.selectBuildTarget);
                                        BuildAssetBundleWithLogging(options);
                                    });
                            } else {
                                EditorCommon.OpenCheckDialogue("에셋번들 빌드", $"에셋번들 빌드를 진행합니다.\n" +
                                                                          $"{EditorUserBuildSettings.selectedBuildTargetGroup}\n{EditorUserBuildSettings.activeBuildTarget}\n\n" +
                                                                          $"대상 디렉토리 : {config.buildDirectory}/{config.selectBuildTarget}\n\n" +
                                                                          $"활성화된 옵션\n{options.ToString()}", ok: () => BuildAssetBundleWithLogging(options));
                            }
                        }
                    }
                }

                if (config.isLogBuildSetting) {
                    GUILayout.Space(5f);
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.Label("메모", Constants.Editor.TITLE_STYLE, GUILayout.Height(100f), GUILayout.Width(50f));
                        _buildInfoMemo = EditorGUILayout.TextArea(_buildInfoMemo, GUILayout.Height(100f), GUILayout.ExpandWidth(true));
                    }
                }
            }

            if (config.GetInfoCount() > 0) {
                using (new GUILayout.VerticalScope(Constants.Editor.BOX)) {
                    var info = config[_buildInfoCursor];
                    using (new GUILayout.HorizontalScope()) {
                        EditorGUI.BeginDisabledGroup(_buildInfoCursor <= 0);
                        if (GUILayout.Button("<", GUILayout.Height(30f))) {
                            _buildInfoCursor = Math.Max(0, _buildInfoCursor - 1);
                        }
                        EditorGUI.EndDisabledGroup();
                        
                        GUILayout.Label($"빌드 기록 [{_buildInfoCursor + 1} / {config.GetInfoCount()}]", Constants.Editor.TITLE_STYLE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(200f));

                        EditorGUI.BeginDisabledGroup(config.GetInfoCount() <= 1 || _buildInfoCursor >= config.GetInfoCount() - 1);
                        if (GUILayout.Button(">", GUILayout.Height(30f))) {
                            _buildInfoCursor = Math.Min(config.GetInfoCount(), _buildInfoCursor + 1);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.Space(5f);

                    EditorCommon.DrawLabelTextField("결과", info.buildSuccess ? "성공".GetColorString(Color.green) : "실패".GetColorString(Color.red));
                    EditorCommon.DrawLabelTextField("타겟", info.buildTarget.ToString());
                    EditorCommon.DrawLabelTextField("경로", info.buildPath);
                    EditorCommon.DrawLabelTextField("시작", info.buildStartTime.ToString(CultureInfo.CurrentCulture));
                    EditorCommon.DrawLabelTextField("종료", info.buildEndTime.ToString(CultureInfo.CurrentCulture));
                    EditorCommon.DrawLabelTextField("시간", info.GetBuildTime().ToString());

                    GUILayout.Space(5f);
                    
                    _buildInfoMemoScrollViewPosition = GUILayout.BeginScrollView(_buildInfoMemoScrollViewPosition, false, false, GUILayout.Height(250f));
                    EditorGUILayout.TextArea(info.memo, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    GUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.EndScrollView();
        } else {
            EditorGUILayout.HelpBox($"{nameof(config)} 파일을 찾을 수 없거나 생성할 수 없습니다.", MessageType.Error);
        }
    }

    private void RefreshAssetBundleList() {
        _allAssetBundleList = AssetDatabase.GetAllAssetBundleNames().Where(Path.HasExtension).Distinct().ToList();
        _allAssetBundleList.ForEach(name => config.selectAssetBundleDic.TryAdd(name, false));
    }
    
    private AssetBundleManifest BuildAssetBundleWithLogging(BuildAssetBundleOptions options) {
        var info = new AssetBundleBuildInfo {
            buildStartTime = DateTime.Now,
            buildPath = config.GetBuildPath()
        };
        
        if (Service.TryGetServiceWithStart<LogCollectorService>(out var service)) {
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
        if (ResourceGenerator.TryGenerateAssetBundle(buildPath, options, config.selectBuildTarget, out var manifest)) {
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
                config.selectAssetBundleDic.Where(pair => pair.Value).Select(pair => pair.Key).ForEach(name => EncryptAssetBundle(buildPath, name));
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

    private AssetBundleChecksumInfo GenerateChecksumInfo(AssetBundleManifest manifest, string buildPath) {
        var info = new AssetBundleChecksumInfo {
            generateTime = DateTime.Now
        };
                
        if (BuildPipeline.GetCRCForAssetBundle(Path.Combine(buildPath, config.selectBuildTarget.ToString()), out var crc)) {
            info.crcDic.AutoAdd(config.selectBuildTarget.ToString(), crc);
        }
                
        foreach (var name in manifest.GetAllAssetBundles()) {
            if (BuildPipeline.GetCRCForAssetBundle(Path.Combine(buildPath, name), out crc)) {
                info.crcDic.AutoAdd(name, crc);
                info.hashDic.AutoAdd(name, manifest.GetAssetBundleHash(name).ToString());
            }
        }

        return info;
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
}

public struct AssetBundleBuildInfo {

    public bool buildSuccess;
    public BuildTarget buildTarget;
    public DateTime buildStartTime;
    public DateTime buildEndTime;
    public string buildPath;
    public string memo;

    public TimeSpan GetBuildTime() => buildEndTime - buildStartTime;
    public override string ToString() => $"{buildStartTime} ==> {buildEndTime} [{buildSuccess.ToString()}]";
}


public class AssetBundleProviderConfig : JsonAutoConfig {

    public string cipherEncryptKey = "";
    public string buildDirectory = "";
    public bool isClearAssetBundleManifest = true;
    public bool isLogBuildSetting = true;
    
    public bool isAssetBundleManifestEncrypted;
    public bool isAssetBundleEncrypted;
    public bool isAssetBundleSelectableEncrypted;
    public readonly Dictionary<string, bool> selectAssetBundleDic = new();
    
    public bool isGenerateChecksumInfo;
    public bool isEncryptChecksum;
    public string checksumFileName = nameof(AssetBundleChecksumInfo);

    public BuildTarget selectBuildTarget;
    public readonly Dictionary<BuildAssetBundleOptions, bool> buildOptionDic = EnumUtil.GetValues<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);
    
    
    [JsonProperty("lastBuildInfoList")]
    private readonly List<AssetBundleBuildInfo> _lastBuildInfoList = new();
    
    private const int MAX_BUILD_LOG = 20;

    public AssetBundleBuildInfo this[int index] => _lastBuildInfoList[index];
    public void AddBuildInfo(AssetBundleBuildInfo info) => _lastBuildInfoList.LimitedAdd(info, MAX_BUILD_LOG);
    public int GetInfoCount() => _lastBuildInfoList.Count;
    public string GetBuildPath() => $"{buildDirectory}/{selectBuildTarget}";
    
    public bool IsActiveAssetBundleEncrypt() => isAssetBundleEncrypted || isAssetBundleSelectableEncrypted;
    public bool IsActiveEncrypt() =>isAssetBundleManifestEncrypted || isAssetBundleEncrypted || isAssetBundleSelectableEncrypted || isEncryptChecksum;

    public override bool IsNull() => this is NullConfig;
    public class NullConfig : AssetBundleProviderConfig { }
}

public record AssetBundleChecksumInfo {

    public DateTime generateTime;
    public Dictionary<string, uint> crcDic = new();
    public Dictionary<string, string> hashDic = new();
}