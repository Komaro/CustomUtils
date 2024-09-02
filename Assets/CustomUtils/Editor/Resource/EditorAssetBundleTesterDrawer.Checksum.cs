using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public partial class EditorAssetBundleTesterDrawer {
    
    private void _DrawChecksum() {
        EditorGUILayout.LabelField("체크썸(Checksum)", Constants.Draw.AREA_TITLE_STYLE);
        using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
            using (var group = new EditorGUILayout.ToggleGroupScope("Test Checksum Group", config.isActiveChecksum)) {
                config.isActiveChecksum = group.enabled;
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isActiveCrc, "CRC 체크 활성화", 150f);
                    EditorCommon.DrawLabelToggle(ref config.isActiveHash, "Hash 체크 활성화", 150f);
                    GUILayout.FlexibleSpace();
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawChecksum() {
        GUILayout.Label("체크썸(Checksum)", Constants.Draw.AREA_TITLE_STYLE);
        using (new GUILayout.VerticalScope("box")) {
            EditorCommon.DrawLabelToggle(ref config.isActiveChecksum, "Checksum 활성화", 150f);

            if (config.isActiveChecksum) {
                using (new GUILayout.HorizontalScope()) {
                    EditorCommon.DrawLabelToggle(ref config.isActiveCrc, "CRC 체크 활성화", 150f);
                    EditorCommon.DrawLabelToggle(ref config.isActiveHash, "Hash 체크 활성화", 150f);
                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.VerticalScope()) {
                    EditorGUI.BeginChangeCheck();
                    using (new GUILayout.HorizontalScope()) {
                        EditorCommon.DrawLabelToggle(ref config.isActiveAutoTrackingChecksum, "Checksum 자동 탐색", 150f);
                        if (config.isActiveAutoTrackingChecksum) {
                            EditorCommon.DrawLabelToggle(ref config.isActiveAllDirectoriesSearch, "하위폴더 탐색 활성화", 150f);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    if (config.isActiveAutoTrackingChecksum) {
                        EditorCommon.DrawFolderOpenSelector("탐색 폴더", "선택", ref config.checksumInfoTrackingDirectory, 120f);
                        EditorCommon.DrawLabelTextField("Json 파일 경로", config.checksumInfoPath, 120f);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        if (config.isActiveAutoTrackingChecksum && Directory.Exists(config.checksumInfoTrackingDirectory)) {
                            SearchLatestChecksumInfo();
                            StartChecksumInfoAutoSearch();
                        } else {
                            StopChecksumInfoAutoSearch();
                        }
                    }

                    if (config.isActiveAutoTrackingChecksum == false) {
                        EditorCommon.DrawFileOpenSelector(ref config.checksumInfoPath, "로컬 Json 파일", "선택", "json", () => LoadAssetBundleChecksumInfo(config.checksumInfoPath));
                        EditorCommon.DrawButtonTextField("Checksum Info Json 다운로드", ref config.checksumInfoDownloadPath, () => {
                            config.checksumInfoDownloadPath = config.checksumInfoDownloadPath.AutoSwitchExtension(Constants.Extension.JSON);
                            DownloadJsonFile<AssetBundleChecksumInfo>(config.checksumInfoDownloadPath, (result, info) => {
                                if (result == UnityWebRequest.Result.Success && info != null) {
                                    Logger.TraceLog($"Successfully downloaded a JSON file in the form of {nameof(AssetBundleChecksumInfo)}", Color.cyan);
                                    _bindChecksumInfo = info;
                                }
                            });
                        });
                    }
                }
            }
        }

        using (new GUILayout.VerticalScope("box")) {
            EditorCommon.DrawLabelTextField("연결 상태", _bindChecksumInfo != null ? "연결".GetColorString(Color.green) : "미연결".GetColorString(Color.red));
            if (_bindChecksumInfo != null) {
                EditorCommon.DrawLabelTextField("생성 시간", _bindChecksumInfo.generateTime.ToString(CultureInfo.CurrentCulture));
                _checksumInfoFold = EditorGUILayout.BeginFoldoutHeaderGroup(_checksumInfoFold, string.Empty);
                if (_checksumInfoFold) {
                    foreach (var pair in _bindChecksumInfo.crcDic) {
                        EditorCommon.DrawLabelTextField(pair.Key, pair.Value.ToString(), 200f);
                    }

                    EditorCommon.DrawSeparator();

                    foreach (var pair in _bindChecksumInfo.hashDic) {
                        EditorCommon.DrawLabelTextField(pair.Key, pair.Value, 200f);
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
    }

    private void LoadAssetBundleChecksumInfo(string path) {
        if (JsonUtil.TryLoadJsonOrDecrypt<AssetBundleChecksumInfo>(out var info, path, _plainEncryptKey)) {
            _bindChecksumInfo = info;
            config.checksumInfoPath = path;
        } else {
            Logger.TraceError($"{path} is Invalid {nameof(AssetBundleChecksumInfo)}");
        }
    }

    private void SearchLatestChecksumInfo() {
        if (Directory.Exists(config.checksumInfoTrackingDirectory)) {
            var filePaths = Directory.GetFiles(config.checksumInfoTrackingDirectory, Constants.Extension.JSON_FILTER,
                config.isActiveAllDirectoriesSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (filePaths.Any()) {
                var path = filePaths.OrderByDescending(File.GetLastWriteTime).First();
                LoadAssetBundleChecksumInfo(path);
            }
        }
    }

    private void StartChecksumInfoAutoSearch() {
        if (Service.TryGetService<SystemWatcherService>(out var service)) {
            service.Remove(_infoAutoTrackingOrder);
            _infoAutoTrackingOrder.path = config.checksumInfoTrackingDirectory;
            service.Start(_infoAutoTrackingOrder);
        }
    }

    private void StopChecksumInfoAutoSearch() {
        if (Service.TryGetService<SystemWatcherService>(out var service)) {
            service.Stop(_infoAutoTrackingOrder);
        }
    }
    
    private void OnFileSystemEventHandler(object _, FileSystemEventArgs args) {
        switch (args.ChangeType) {
            case WatcherChangeTypes.Deleted:
                if (config.checksumInfoPath.EqualsFast(args.FullPath)) {
                    SearchLatestChecksumInfo();
                }
                break;
            default:
                if (_bindChecksumInfo == null) {
                    LoadAssetBundleChecksumInfo(args.FullPath);
                } else {
                    if (_bindChecksumInfo.generateTime > File.GetCreationTime(args.FullPath)) {
                        LoadAssetBundleChecksumInfo(args.FullPath);
                    }
                }
                break;
        }
    }
}