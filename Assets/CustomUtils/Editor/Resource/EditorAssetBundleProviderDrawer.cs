using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Provider, RESOURCE_SERVICE_TYPE.AssetBundle)]
public partial class EditorAssetBundleProviderDrawer : EditorResourceDrawer<AssetBundleProviderConfig, AssetBundleProviderConfig.NullConfig> {

    private int _buildInfoCursor;
    private string _buildInfoMemo;
    private bool _isShowEncryptKey;
    private string _plainEncryptKey;
    
    private static AssetBundleTreeView _assetBundleTreeView = new();
    
    private bool _assetBundleListFold;
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _buildOptionScrollViewPosition;
    private Vector2 _buildInfoMemoScrollViewPosition;
    
    private readonly List<BuildAssetBundleOptions> BUILD_OPTION_LIST = EnumUtil.GetValueList<BuildAssetBundleOptions>(true, true);

    protected override string CONFIG_NAME => $"{nameof(AssetBundleProviderConfig)}{Constants.Extension.JSON}";
    
    public EditorAssetBundleProviderDrawer(EditorWindow window) : base(window) { }
    
    public override sealed void CacheRefresh() {
        Service.GetService<SystemWatcherService>().Start(order);
        if (JsonUtil.TryLoadJson(CONFIG_PATH, out config)) {
            _plainEncryptKey = string.IsNullOrEmpty(config.cipherEncryptKey) == false ? EncryptUtil.DecryptDES(config.cipherEncryptKey) : string.Empty;
            config.StartAutoSave(CONFIG_PATH);
        } else {
            if (config == null || config.IsNull() == false) {
                config = new AssetBundleProviderConfig.NullConfig();
            }
        }
        
        _buildInfoCursor = config.GetInfoCount() - 1;
        BUILD_OPTION_LIST.ForEach(option => config.buildOptionDic.TryAdd(option, false));
        
        _assetBundleTreeView.SetConfig(config);
        RefreshAssetBundle();
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
            
            GUILayout.Label("AssetBundle 빌드 폴더", Constants.Draw.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawFolderOpenSelector("빌드 폴더", "선택", ref config.buildDirectory, width:60f);
            }
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label("보안", Constants.Draw.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
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
            
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
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

            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                if (config.IsActiveAssetBundleEncrypt()) {
                    EditorGUILayout.HelpBox("AssetBundle 암호화 옵션은 하나만 선택이 가능합니다. 다른 옵션을 활성화 하기 위해선 현재 활성화 되어 있는 옵션을 비활성화 해야 합니다.", MessageType.Info);
                }

                if (config.IsActiveEncrypt()) {
                    EditorGUILayout.HelpBox($"암호화가 활성화 되었습니다. {nameof(Caching)} 기능을 사용할 수 없습니다.", MessageType.Warning);
                }
            }
            
            EditorCommon.DrawSeparator();
            
            if (EditorCommon.DrawLabelButton("전체 AssetBundle 리스트", Constants.Draw.REFRESH_ICON, Constants.Draw.AREA_TITLE_STYLE)) {
                RefreshAssetBundle();
            }
            
            DrawAssetBundleList();
            
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                if (config.IsActiveEncrypt()) {
                    if (config.IsNull()) {
                        EditorGUILayout.HelpBox($"{nameof(AssetBundleProviderConfig)} 파일이 존재하지 않기 때문에 저장되지 않습니다.", MessageType.Warning);
                    }
                    
                    EditorCommon.DrawButtonPasswordField("Encrypt Key 저장", ref _plainEncryptKey, ref _isShowEncryptKey, plainEncryptKey => config.cipherEncryptKey = EncryptUtil.EncryptDES(plainEncryptKey), 110f);
                }
            }

            EditorCommon.DrawSeparator();

            GUILayout.Label("AssetBundle 빌드 옵션", Constants.Draw.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                _buildOptionScrollViewPosition = GUILayout.BeginScrollView(_buildOptionScrollViewPosition, false, false, GUILayout.Height(200f));
                foreach (var option in BUILD_OPTION_LIST) {
                    config.buildOptionDic[option] = EditorCommon.DrawLabelToggle(config.buildOptionDic[option], option.ToString());
                    GUILayout.Space(1);
                }

                GUILayout.EndScrollView();
            }
            
            EditorCommon.DrawSeparator();

            GUILayout.Label("빌드", Constants.Draw.AREA_TITLE_STYLE);
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isClearAssetBundleManifest, ".manifest 제거", "빌드 완료 후 .manifest 확장자 파일 제거", 180f);
                    EditorCommon.DrawLabelToggle(ref config.isLogBuildSetting, "빌드 세팅 기록", $"빌드 완료 후 {nameof(AssetBundleProviderConfig)}를 메모에 기록", 180f);
                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isSelectableBuild, "선택적 빌드 활성화", "선택적 빌드를 체크한 에셋번들만 빌드", 180f);
                    GUILayout.FlexibleSpace();
                }
            }
            
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                using (new GUILayout.HorizontalScope()) {
                    using (new GUILayout.VerticalScope()) {
                        EditorCommon.DrawEnumPopup(string.Empty, ref config.selectBuildTarget, 0, GUILayout.Height(20f));
                        EditorCommon.DrawLabelTextField("현재 빌드 타겟", EditorUserBuildSettings.activeBuildTarget.ToString());
                    }

                    if (config.IsActiveEncrypt()) {
                        if (string.IsNullOrEmpty(_plainEncryptKey)) {
                            EditorGUILayout.HelpBox("암호화 옵션이 활성화 되어 있습니다. 암호화에 필요한 키를 입력하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                        } else if (config.buildOptionDic.IsTrue(BuildAssetBundleOptions.ForceRebuildAssetBundle) == false) {
                            EditorGUILayout.HelpBox($"{BuildAssetBundleOptions.ForceRebuildAssetBundle} 활성화 되어 있지 않습니다. 변경사항이 없는 에셋번들의 경우 중복으로 암호화가 적용될 수 있습니다.", MessageType.Error);
                        } else {
                            DrawBuildButton();
                        }
                    } else if (string.IsNullOrEmpty(config.buildDirectory)) {
                        EditorGUILayout.HelpBox("빌드 폴더를 선택하여야 에셋번들 빌드를 진행할 수 있습니다.", MessageType.Error);
                    } else {
                        DrawBuildButton();
                    }
                }

                if (config.isLogBuildSetting) {
                    GUILayout.Space(5f);
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.Label("메모", Constants.Draw.TITLE_STYLE, GUILayout.Height(100f), GUILayout.Width(50f));
                        _buildInfoMemo = EditorGUILayout.TextArea(_buildInfoMemo, GUILayout.Height(100f), GUILayout.ExpandWidth(true));
                    }
                }
            }

            if (config.GetInfoCount() > 0) {
                using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                    using (new GUILayout.HorizontalScope()) {
                        EditorGUI.BeginDisabledGroup(_buildInfoCursor <= 0);
                        if (GUILayout.Button("<", GUILayout.Height(30f))) {
                            _buildInfoCursor = Math.Max(0, _buildInfoCursor - 1);
                        }
                        EditorGUI.EndDisabledGroup();
                        
                        GUILayout.Label($"빌드 기록 [{_buildInfoCursor + 1} / {config.GetInfoCount()}]", Constants.Draw.TITLE_STYLE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(200f));

                        EditorGUI.BeginDisabledGroup(config.GetInfoCount() <= 1 || _buildInfoCursor >= config.GetInfoCount() - 1);
                        if (GUILayout.Button(">", GUILayout.Height(30f))) {
                            _buildInfoCursor = Math.Min(config.GetInfoCount(), _buildInfoCursor + 1);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.Space(5f);

                    var info = config[_buildInfoCursor];
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

    private void DrawAssetBundleList() {
        using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
            _assetBundleListFold = EditorGUILayout.BeginFoldoutHeaderGroup(_assetBundleListFold, string.Empty);
            if (_assetBundleListFold && config.assetBundleInfoDic != null) {
                _assetBundleTreeView.Draw();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

    private AssetBundleChecksumInfo GenerateChecksumInfo(AssetBundleManifest manifest, string buildPath) {
        var info = new AssetBundleChecksumInfo {
            generateTime = DateTime.Now,
            target = config.selectBuildTarget.ToString(),
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
    
    private void RefreshAssetBundle() {
        config.RefreshAssetBundle();
        config.assetBundleInfoDic.Values.ForEach(info => _assetBundleTreeView.Add(info));
        _assetBundleTreeView.Reload();
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
    public bool isSelectableBuild = false;
    
    public bool isAssetBundleManifestEncrypted;
    public bool isAssetBundleEncrypted;
    public bool isAssetBundleSelectableEncrypted;
    public readonly SortedDictionary<string, AssetBundleSelectableInfo> assetBundleInfoDic = new();

    public bool isGenerateChecksumInfo;
    public bool isEncryptChecksum;
    public string checksumFileName = nameof(AssetBundleChecksumInfo);

    public BuildTarget selectBuildTarget;
    public readonly Dictionary<BuildAssetBundleOptions, bool> buildOptionDic = EnumUtil.GetValueList<BuildAssetBundleOptions>(true).ToDictionary(x => x, _ => false);
    
    #region [AssetBundle Build Info]
    
    [JsonProperty("lastBuildInfoList")]
    private readonly List<AssetBundleBuildInfo> _lastBuildInfoList = new();
    
    private const int MAX_BUILD_LOG = 20;
    
    public AssetBundleBuildInfo this[int index] => _lastBuildInfoList[index];
    public void AddBuildInfo(AssetBundleBuildInfo info) => _lastBuildInfoList.LimitedAdd(info, MAX_BUILD_LOG);
    public int GetInfoCount() => _lastBuildInfoList.Count;
    
    #endregion
    
    public void RefreshAssetBundle() => assetBundleInfoDic.Sync(AssetDatabase.GetAllAssetBundleNames().Where(Path.HasExtension).ToHashSet(), key => new AssetBundleSelectableInfo(key));
    public string GetBuildPath() => $"{buildDirectory}/{selectBuildTarget}";
    
    public bool IsActiveAssetBundleEncrypt() => isAssetBundleEncrypted || isAssetBundleSelectableEncrypted;
    public bool IsActiveEncrypt() => isAssetBundleManifestEncrypted || isAssetBundleEncrypted || isAssetBundleSelectableEncrypted;

    public override bool IsNull() => this is NullConfig;
    public class NullConfig : AssetBundleProviderConfig { }
}

public record AssetBundleSelectableInfo {
    
    public string name;
    public bool isSelect;
    public bool isEncrypt;

    public AssetBundleSelectableInfo(string name) {
        this.name = name;
        isEncrypt = false;
    }
}

#region [TreeView]

internal class AssetBundleTreeView : EditorServiceTreeView {

    private readonly Dictionary<string, TreeViewItem> _itemDic = new();
    
    private AssetBundleProviderConfig _bindConfig;
    
    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("이름", 150f, textAlignment:TextAlignment.Left),
        CreateColumn("선택적 암호화", 90f, 90f),
        CreateColumn("선택적 빌드", 90f, 90f),
    };

    public AssetBundleTreeView() : base(new MultiColumnHeader(new MultiColumnHeaderState(COLUMNS))) { }
    protected override TreeViewItem BuildRoot() => new() { id = 0, depth = -1, children = _itemDic.ToValueList() };
    
    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AssetBundleTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.info.name);

            using (new EditorGUI.DisabledGroupScope(_bindConfig is not { isAssetBundleSelectableEncrypted: not false })) {
                EditorCommon.DrawFitToggle(args.GetCellRect(2), ref item.info.isEncrypt);
            }

            using (new EditorGUI.DisabledGroupScope(_bindConfig is not { isSelectableBuild: not false})) {
                EditorCommon.DrawFitToggle(args.GetCellRect(3), ref item.info.isSelect);
            }
        }
    }

    public void Add(AssetBundleSelectableInfo info) {
        if (_itemDic.ContainsKey(info.name) == false) {
            _itemDic.Add(info.name, new AssetBundleTreeViewItem(_itemDic.Count, info));
        }
    }

    protected override IEnumerable<TreeViewItem> GetOrderBy(int index, bool isAscending) {
        if (rootItem.children.TryCast<AssetBundleTreeViewItem>(out var enumerable)) {
            enumerable = EnumUtil.Convert<SORT_TYPE>(index) switch {
                SORT_TYPE.NO => enumerable.OrderBy(x => x.id, isAscending),
                SORT_TYPE.NAME => enumerable.OrderBy(x => x.info.name, isAscending),
                SORT_TYPE.SELECTABLE_ENCRYPT => enumerable.OrderBy(x => x.info.isEncrypt, isAscending),
                _ => enumerable
            };

            return enumerable;
        }

        return Enumerable.Empty<TreeViewItem>();
    }

    public void SetConfig(AssetBundleProviderConfig config) => _bindConfig = config;

    protected override bool OnDoesItemMatchSearch(TreeViewItem item, string search) => item is AssetBundleTreeViewItem bundleItem && bundleItem.info.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    private sealed class AssetBundleTreeViewItem : TreeViewItem {
    
        public AssetBundleSelectableInfo info;

        public AssetBundleTreeViewItem(int id, AssetBundleSelectableInfo info) {
            this.id = id;
            this.info = info;
            depth = 0;
        }
    }

    private enum SORT_TYPE {
        NO,
        NAME,
        SELECTABLE_ENCRYPT,
    }
}

#endregion