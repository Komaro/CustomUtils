using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorTempWebServer : EditorWindow {
    
    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorTempWebServer>("TempWebServer") : _window;

    private HttpListener _listener;

    private static string _targetDirectory;
    private static bool _isCustomURL;
    private static string _url;

    private static string _configPath;
    private static Config _config;

    private Regex _urlRegex = new(@"^(http|https|ftp)://[^\s/$.?#].[^\s]*$");

    private const string CONFIG_NAME = "TempWebServerConfig.json";
    private const string LOCAL_HOST_URL = "http://localhost:8000/";
    
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
        _isCustomURL = false;
        _url = LOCAL_HOST_URL;
        
        _config = null;
        if (File.Exists(_configPath) && EditorCommon.TryLoadJson(_configPath, out _config)) {
            _targetDirectory = _config.targetDirectory;
            _url = _config.url;
        }
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
        
        if (_urlRegex.IsMatch(_url) == false) {
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
                    if (_listener?.IsListening ?? false) {
                        EditorCommon.ShowCheckDialogue("경고", "현재 임시 웹 서버가 동작중입니다.\n확인 시 종료 후 서버를 재시작합니다.", ok: () => {
                            StopWebServer();
                            CloseWebServer();
                            StartWebServer();
                        });
                    } else {
                        StartWebServer();
                    }
                }
                
                if (GUILayout.Button("서버\n중단", GUILayout.Height(50))) {
                    StopWebServer();
                }
            }
            
            if (_listener != null) {
                if (GUILayout.Button("서버 제거", GUILayout.Height(35))) {
                    StopWebServer();
                    CloseWebServer();
                }
            }

        }
        
    }

    private void StartWebServer() {
        StopWebServer();
        
        _listener = new HttpListener();
        _listener.Prefixes.Add(_url);

        _listener.Start();

        // TODO. 스레드 구현으로 변경
        
        Logger.TraceLog("Start Web Server(CDN)", Color.green);
    }

    private void StopWebServer() {
        if (_listener?.IsListening ?? false) {
            Logger.TraceLog("Stop Web Server(CDN)", Color.yellow);
            _listener.Stop();
        }
    }

    private void CloseWebServer() {
        if (_listener != null) {
            Logger.TraceLog("Close Web Server(CDN)", Color.red);
            _listener.Close();
            _listener = null;
        }
    }

    private void SaveConfig() {
        if (_config != null) {
            EditorCommon.SaveJson(_configPath, _config);
        }
    }

    private class Config {
        
        public string targetDirectory = "";
        public string url = "http://localhost:8000/";
    }
}

