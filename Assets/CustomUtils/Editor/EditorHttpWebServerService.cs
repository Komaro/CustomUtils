using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorHttpWebServerService : EditorService {
    
    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorHttpWebServerService>("HttpWebServer Service") : _window;

    private SimpleHttpWebServer _httpServer;
    
    private static string _targetDirectory;
    private static string _url;

    private static string _configPath;
    private static Config _config;

    private static List<Type> _serveModuleList = new();
    
    private Vector2 _windowScrollViewPosition;
    private Vector2 _serveModulePresetScrollPosition;
    private Vector2 _runningServeModuleScrollPosition;
    private Vector2 _serveModuleScrollPosition;
    
    private const string CONFIG_NAME = "HttpWebServerServiceConfig.json";
    private readonly Regex URL_REGEX = new(@"^(http|https|ftp)://[^\s/$.?#].[^\s]*$");

    public EditorHttpWebServerService() {
        AssemblyReloadEvents.beforeAssemblyReload += () => _httpServer?.Dispose();
    }
    
    protected override void OnEditorOpenInitialize() => CacheRefresh();

    [MenuItem("Service/Temp Web Server/Http Web Server Service")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (string.IsNullOrEmpty(_configPath)) {
            _configPath = $"{Constants.Path.COMMON_CONFIG_FOLDER}/{CONFIG_NAME}";
        }

        _config = null;
        if (File.Exists(_configPath) && JsonUtil.TryLoadJson(_configPath, out _config)) {
            _targetDirectory = _config.targetDirectory;
            _url = _config.url;
        }

        _serveModuleList = ReflectionManager.GetSubClassTypes<HttpServeModule>().ToList();
    }
    
    private void OnGUI() {
        if (_config == null) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 서버 설정이 저장되지 않으며 일부 기능을 사용할 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button("Config 파일 생성")) {
                EditorCommon.OpenCheckDialogue("Config 파일 생성", $"Config 파일을 생성합니다.\n경로는 아래와 같습니다.\n{_configPath}", ok: () => {
                    _config = new Config();
                    _config.Save(_configPath);
                });
            }
            
            EditorCommon.DrawSeparator();
        }

        _windowScrollViewPosition = EditorGUILayout.BeginScrollView(_windowScrollViewPosition, false, false);
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button("CDN 폴더 선택", GUILayout.Width(100))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("대상 폴더", _targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    _targetDirectory = selectDirectory;
                }
                
                if (_config != null) {
                    _config.targetDirectory = _targetDirectory;
                    _config.Save(_configPath);
                }
            }
            
            GUILayout.TextField(_targetDirectory);
        }

        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label("URL", Constants.Editor.TITLE_STYLE, GUILayout.Width(100));
            _url = GUILayout.TextField(_url);
        }
        
        if (URL_REGEX.IsMatch(_url) == false) {
            EditorGUILayout.HelpBox("유효하지 않은 URL 주소입니다.\n주소는 아래와 같이 시작하여야 합니다.\nhttp:// https:// ftp://", MessageType.Warning);
        } else {
            if (_config != null && _config.url != _url && GUILayout.Button("Custom URL 저장")) {
                _config.url = _url;
                _config.Save(_configPath);
            }
        }

        using (new GUILayout.VerticalScope()) {
            using (new GUILayout.HorizontalScope()) {
                if (GUILayout.Button("서버\n시작", GUILayout.Height(50))) {
                    StartHttpWebServer();
                }
                
                if (GUILayout.Button("서버\n중단", GUILayout.Height(50))) {
                    _httpServer?.Stop();
                }
            }

            if (_httpServer != null) {
                if (GUILayout.Button("서버 제거", GUILayout.Height(35))) {
                    _httpServer.Close();
                    _httpServer = null;
                }
            }

            GUILayout.Space(10);
            
            if (_config != null) {
                using (new GUILayout.VerticalScope("box")) {
                    GUILayout.Label("=== 서버 모듈 프리셋 ===", Constants.Editor.DIVIDE_STYLE);

                    ModulePreset removePreset = null;
                    _serveModulePresetScrollPosition = EditorGUILayout.BeginScrollView(_serveModulePresetScrollPosition, false, false, GUILayout.MinHeight(50f), GUILayout.MaxHeight(150f));
                    foreach (var modulePreset in _config.modulePresetList) {
                        using (new GUILayout.HorizontalScope()) {
                            var moduleTextContent = new GUIContent(modulePreset.moduleNameList.ToStringCollection("\n"));
                            var heightOption = GUILayout.Height(GUI.skin.button.CalcSize(moduleTextContent).y);
                            if (GUILayout.Button("X", GUILayout.MaxWidth(30f), heightOption)) {
                                removePreset = modulePreset;
                            }

                            if (GUILayout.Button(moduleTextContent, heightOption)) {
                                StartHttpWebServer();
                                foreach (var moduleName in modulePreset.moduleNameList) {
                                    if (_serveModuleList.TryFind(x => x.Name == moduleName, out var type)) {
                                        _httpServer.AddServeModule(type);
                                    }
                                }

                                _config.Save(_configPath);
                            }
                        }
                    }

                    if (removePreset != null) {
                        _config.RemovePreset(removePreset);
                        _config.Save(_configPath);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            GUILayout.Space(10);
            
            using (new GUILayout.VerticalScope("box", GUILayout.ExpandHeight(false))) {
                GUILayout.Label("=== Web Server Run Info ===", Constants.Editor.DIVIDE_STYLE);
                GUILayout.Label(_httpServer == null ? "미생성" : _httpServer.IsRunning() ? "가동중" : "정지", Constants.Editor.TITLE_STYLE);
                if (_httpServer != null) {
                    EditorCommon.DrawLabelLinkButton("대상 폴더 : ", _httpServer.GetTargetDirectory(), EditorUtility.RevealInFinder, 60f);
                    EditorCommon.DrawLabelSelectableLabel("URL : ", _httpServer.GetURL(), 40f);
                    
                    GUILayout.Space(10);
                    
                    GUILayout.Label("가동 중인 모듈", Constants.Editor.DIVIDE_STYLE);
                    _runningServeModuleScrollPosition = EditorGUILayout.BeginScrollView(_runningServeModuleScrollPosition, false, false, GUILayout.MinHeight(50f), GUILayout.MaxHeight(100f));
                    foreach (var type in _httpServer.GetServeModuleTypeList()) {
                        GUILayout.Label(type.Name, Constants.Editor.TITLE_STYLE);
                    }
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("모듈 전체 제거")) {
                        _httpServer.ClearServeModule();
                    }
                    
                    if (GUILayout.Button("모듈 프리셋 저장", GUILayout.Height(40))) {
                        if (_config != null && _httpServer.GetServeModuleTypeList().Any()) {
                            _config.AddPreset(new ModulePreset(_httpServer.GetServeModuleTypeList().Select(x => x.Name).ToList()));
                            _config.Save(_configPath);
                        }
                    }
                }
            }
            
            GUILayout.Space(15);
            _serveModuleScrollPosition = EditorGUILayout.BeginScrollView(_serveModuleScrollPosition, false, false, GUILayout.MinHeight(50f), GUILayout.MaxHeight(150f));
            using (new GUILayout.VerticalScope()) {
                GUILayout.Label($"{nameof(HttpServeModule)} 추가", Constants.Editor.DIVIDE_STYLE);
                foreach (var type in _serveModuleList) {
                    if (_httpServer == null) {
                        GUILayout.Button(type.Name);
                    } else {
                        var alreadyModule = _httpServer.IsContainsServeModule(type);
                        if (GUILayout.Button($"{type.Name} {(alreadyModule ? "제거" : "추가")}")) {
                            if (_httpServer != null) {
                                if (alreadyModule) {
                                    _httpServer.RemoveServeModule(type);
                                } else {
                                    _httpServer.AddServeModule(type);
                                }
                            }
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void StartHttpWebServer() {
        _httpServer ??= new SimpleHttpWebServer(_url);
        if (_httpServer.IsRunning()) {
            EditorCommon.OpenCheckDialogue("경고", "현재 임시 웹 서버가 동작중입니다.\n확인 시 종료 후 서버를 재시작합니다.", ok: () => {
                _httpServer.Restart();
            });
        } else {
            _httpServer.Start(_targetDirectory);
        }
    }
    
    private class NullConfig : Config { }
    
    private class Config : JsonConfig {
        
        public string targetDirectory = "";
        public string url = "http://localhost:8000/";
        public List<ModulePreset> modulePresetList = new();
        
        public void AddPreset(ModulePreset addPreset) {
            foreach (var preset in modulePresetList) {
                if (preset.IsMatch(addPreset)) {
                    return;
                }
            }
            
            modulePresetList.Add(addPreset);
        }

        public void RemovePreset(ModulePreset removePreset) {
            if (modulePresetList.TryFindIndex(x => x.IsMatch(removePreset), out var index)) {
                modulePresetList.RemoveAt(index);
            }
        }

        public override bool IsNull() => this is NullConfig;
    }

    private class ModulePreset {
        
        public List<string> moduleNameList = new();
        
        public ModulePreset(List<string> moduleNameList) => this.moduleNameList = moduleNameList;

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
