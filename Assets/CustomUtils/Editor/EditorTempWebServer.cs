using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Networking;

public class EditorTempWebServer : EditorWindow {
    
    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorTempWebServer>("TempWebServer") : _window;

    private SimpleHttpWebServer _httpServer;

    private static string _targetDirectory;
    private static string _url;

    private static string _configPath;
    private static Config _config;

    private static List<Type> _serveModuleList = new();
    private Vector2 _scrollPos;

    private const string CONFIG_NAME = "TempWebServerConfig.json";
    private readonly Regex RUL_REGEX = new(@"^(http|https|ftp)://[^\s/$.?#].[^\s]*$");

    [MenuItem("Service/Temp/Web Server(CDN)")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Editor.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }

        _targetDirectory = string.Empty;
        _url = Constants.Network.DEFAULT_LOCAL_HOST;

        _config = null;
        if (File.Exists(_configPath) && EditorCommon.TryLoadJson(_configPath, out _config)) {
            _targetDirectory = _config.targetDirectory;
            _url = _config.url;
        }

        _serveModuleList = ReflectionManager.GetSubClassTypes<HttpServeModule>().ToList();
    }
    
    private void OnGUI() {
        if (_config == null) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 서버 설정이 저장되지 않습니다.", MessageType.Warning);
            if (GUILayout.Button("Config 파일 생성")) {
                EditorCommon.ShowCheckDialogue("Config 파일 생성", $"Config 파일을 생성합니다.\n경로는 아래와 같습니다.\n{_configPath}", ok: () => {
                    _config = new Config();
                    SaveConfig();
                });
            }
            
            EditorCommon.DrawSeparator();
        }

        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button("CDN 폴더 선택", GUILayout.Width(100))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("대상 폴더", _targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    _targetDirectory = selectDirectory;
                }
                
                if (_config != null) {
                    _config.targetDirectory = _targetDirectory;
                    SaveConfig();
                }
            }
            
            GUILayout.TextField(_targetDirectory);
        }

        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label("URL", Constants.Editor.FIELD_TITLE_STYLE, GUILayout.Width(100));
            _url = GUILayout.TextField(_url);
        }
        
        if (RUL_REGEX.IsMatch(_url) == false) {
            EditorGUILayout.HelpBox("유효하지 않은 URL 주소입니다.\n주소는 아래와 같이 시작하여야 합니다.\nhttp:// https:// ftp://", MessageType.Warning);
        } else {
            if (_config != null && _config.url != _url && GUILayout.Button("Custom URL 저장")) {
                _config.url = _url;
                SaveConfig();
            }
        }

        using (new GUILayout.VerticalScope()) {
            using (new GUILayout.HorizontalScope()) {
                if (GUILayout.Button("서버\n시작", GUILayout.Height(50))) {
                    _httpServer ??= new SimpleHttpWebServer(_url);
                    if (_httpServer.IsRunning()) {
                        EditorCommon.ShowCheckDialogue("경고", "현재 임시 웹 서버가 동작중입니다.\n확인 시 종료 후 서버를 재시작합니다.", ok: () => {
                            _httpServer.Restart();
                        });
                    } else {
                        _httpServer.Start(_targetDirectory);
                    }
                }
                
                if (GUILayout.Button("서버\n중단", GUILayout.Height(50))) {
                    _httpServer?.Stop();
                }
            }

            if (_config != null) {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false, GUILayout.MaxHeight(200));
                foreach (var modulePreset in _config.modulePresetList) {
                    using (new GUILayout.HorizontalScope()) {
                        if (GUILayout.Button("X", GUILayout.MaxWidth(30))) {
                            // TODO. REMOVE PRESET and Test Check
                            _config.RemovePreset(modulePreset);
                        }
                        
                        if (GUILayout.Button(modulePreset.moduleNameList.ToStringCollection("\n"))) {
                            // TODO. CREATE PRESET SERVER
                            if (_httpServer != null) {
                                foreach (var moduleName in modulePreset.moduleNameList) {
                                    // TODO. Get ServeModuleType
                                    
                                    // TODO. Add Serve Module
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(20);
            using (new GUILayout.VerticalScope()) {
                GUILayout.BeginVertical("box");
                GUILayout.Label("== Web Server Run Info == ", Constants.Editor.DIVIDE_STYLE);
                GUILayout.Label(_httpServer == null ? "미생성" : _httpServer.IsRunning() ? "가동중" : "정지", Constants.Editor.FIELD_TITLE_STYLE);
                if (_httpServer != null) {
                    GUILayout.Label($"대상 폴더 : {_httpServer.GetTargetDirectory()}");
                    GUILayout.Label($"URL : {_httpServer.GetURL()}");
                    GUILayout.Label($"가동 중인 쓰레드 : {_httpServer.GetRunningThreadCount()}");
                    
                    GUILayout.Space(10);
                    
                    GUILayout.Label("가동 중인 모듈", Constants.Editor.DIVIDE_STYLE);
                    foreach (var type in _httpServer.GetServeModuleTypeList()) {
                        GUILayout.Label(type.Name, Constants.Editor.FIELD_TITLE_STYLE);
                    }
                }
                
                GUILayout.EndVertical();
            }
            
            if (_httpServer != null) {
                if (GUILayout.Button("현재 모듈 프리셋 저장")) {
                    if (_config != null) {
                        _config.SavePreset(new ModulePreset(_httpServer.GetServeModuleTypeList().Select(x => x.Name).ToList()));
                        SaveConfig();
                    }
                }

                GUILayout.Space(25);
                using (new GUILayout.VerticalScope()) {
                    GUILayout.Label($"{nameof(HttpServeModule)} 추가", Constants.Editor.DIVIDE_STYLE);
                    foreach (var type in _serveModuleList) {
                        var alreadyModule = _httpServer.IsContainsServeModule(type);
                        if (GUILayout.Button($"{type.Name} {(alreadyModule ? "제거" : "추가")}")) {
                            if (alreadyModule) {
                                _httpServer.RemoveServeModule(type);
                            } else {
                                _httpServer.AddServeModule(type);
                            }
                        }
                    }
                }
                
                GUILayout.Space(15);
                
                if (GUILayout.Button("서버 제거", GUILayout.Height(35))) {
                    _httpServer.Close();
                    _httpServer = null;
                }
                
                
                
                
                
                // TODO. Extract Another EditorWindow
                GUILayout.Space(20);
                if (Caching.ready) {
                    if (GUILayout.Button("Caching 클리어")) {
                        AssetBundle.UnloadAllAssetBundles(false);
                        Logger.TraceLog(Caching.ClearCache() ? $"Cache Clear Success || {Caching.currentCacheForWriting.path}" : "Cache Clear Failed", Color.yellow);
                    }
                }

                if (GUILayout.Button("Request Test")) {
                    AssetBundle.UnloadAllAssetBundles(true);

                    var cachePath = $"{Application.persistentDataPath}/AssetBundles";
                    var cache = Caching.GetCacheByPath(cachePath);
                    if (cache.valid == false) {
                        if (Directory.Exists(cachePath) == false) {
                            Directory.CreateDirectory(cachePath);
                        }
                
                        cache = Caching.AddCache(cachePath);
                        Caching.currentCacheForWriting = cache;
                    }

                    TestAssetBundleManifestDownload($"{EditorUserBuildSettings.activeBuildTarget}/{EditorUserBuildSettings.activeBuildTarget}", manifest => {
                        foreach (var assetBundleName in manifest.GetAllAssetBundles()) {
                            var assetBundlePath = $"{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";
                            TestAssetBundleDownload(assetBundlePath, manifest.GetAssetBundleHash(assetBundleName), assetBundle => {
                                // Logger.TraceError($"{assetBundle.name} || {manifest.GetAssetBundleHash(assetBundle.name)}");
                            });
                        }
                    });
                }
            }
        }
    }

    // Caching 안됨
    private void TestAssetBundleManifestDownload(string name, Action<AssetBundleManifest> callback = null) {
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
                
                var data = EncryptUtils.DecryptAES(request.downloadHandler.data);
                var assetBundle = AssetBundle.LoadFromMemory(data);
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
            }
        };
    }
    
    // Caching 됨
    private void TestAssetBundleDownload(string name, Hash128 hash, Action<AssetBundle> callback = null) {
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
    

    private void SaveConfig() {
        if (_config != null) {
            EditorCommon.SaveJson(_configPath, _config);
        }
    }

    private class Config {
        
        public string targetDirectory = "";
        public string url = "http://localhost:8000/";
        public List<ModulePreset> modulePresetList = new();

        public void SavePreset(ModulePreset savePreset) {
            foreach (var preset in modulePresetList) {
                if (preset.IsMatch(savePreset)) {
                    return;
                }
            }
            
            modulePresetList.Add(savePreset);
        }

        public void RemovePreset(ModulePreset removePreset) {
            if (modulePresetList.TryFindIndex(x => x.IsMatch(removePreset), out var index)) {
                modulePresetList.RemoveAt(index);
            }
        }
    }

    private class ModulePreset {
        
        public List<string> moduleNameList = new();
        
        public ModulePreset(List<string> moduleNameList) {
            this.moduleNameList = moduleNameList;
        }

        public bool IsMatch(ModulePreset preset) {
            var aSortList = moduleNameList.OrderBy(x => x).ToList();
            var bSortList = preset.moduleNameList.OrderBy(x => x).ToList();
            if (aSortList.Count != bSortList.Count) {
                return false;
            }
            
            for (var i = 0; i < aSortList.Count; i++) {
                if (aSortList[i] != bSortList[i]) {
                    return false;
                }
            }

            return true;
        }
    }
}
