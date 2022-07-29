using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class EditorConstants {
    public static class Build {
        public static string[] DEFINE_SYMBOLS = {
            "", "=== DEBUG ===",
            
        };
    }
}

public class EditorBuild : EditorWindow {

    private string _buildPath;

    #region [Common]
    private BUILD_TYPE _buildType;
    private string _applicationIdentifier;
    private string _bundleVersion; 
    private string _defineSymbols;
    private StackTraceLogType _stackTraceLogType;
    #endregion

    #region [Android]
    private int _bundleVersionCode;
    private bool _useAPKExpansionFiles;
    private bool _buildAppBundle;
    private bool _buildApkPerCpuArchitecture;
    private bool _androidCreateSymbolsZip;
    private bool _exportAsGoogleAndroidProject;
    #endregion

    #region [iOS]
    private string _buildNumber;
    private string _appleDeveloperTeamID;
    private ProvisioningProfileType _iOSManualProvisioningProfileType;
    private string _iOSManualProvisioningProfileID;
    #endregion

    private static EditorBuild _window;
    public static EditorBuild window => _window == null ? _window = GetWindow<EditorBuild>("Build") : _window;
    private Vector2 _scrollPos;
        
    private static GUIStyle DIVIDE_STYLE = new();
    private static GUIStyle PATH_STYLE = new();
    private static GUIStyle BOLD_STYLE = new();

    private string _unityPath;
    private string UNITY_PATH => string.IsNullOrEmpty(_unityPath) ? _unityPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityPath; 

    [MenuItem("Build/Open Build Setting")]
    private static void OpenWindow() {
        DIVIDE_STYLE.alignment = TextAnchor.MiddleCenter;
        DIVIDE_STYLE.normal.textColor = Color.cyan;
        
        PATH_STYLE.normal.textColor = Color.white;
        PATH_STYLE.fontStyle = FontStyle.Bold;
        PATH_STYLE.fontSize = 12;
        
        BOLD_STYLE.normal.textColor = Color.gray;
        BOLD_STYLE.fontStyle = FontStyle.Bold;
        
        window.position = new Rect(800f, 200f, 400f, 800f);
        window.Show();
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("==================== Build Setting ====================", DIVIDE_STYLE);
        if (GUILayout.Button("Clear Build Path", GUILayout.Width(150f), GUILayout.Height(20f))) {
            _buildPath = string.Empty;
        }
        
        if (GUILayout.Button("Select Build Path", GUILayout.Width(150f), GUILayout.Height(20f))) {
            var beforePath = _buildPath;
            _buildPath = EditorUtility.OpenFolderPanel("Build Path", _buildPath, _buildPath);
            if (string.IsNullOrEmpty(_buildPath)) {
                _buildPath = beforePath;
            }
            
            if (string.IsNullOrEmpty(_buildPath)) {
                _buildPath = $"{UNITY_PATH}/Build";
            }
            
            var regex = new Regex(@$"\/{_buildType}");
            if (regex.IsMatch(_buildPath) == false) {
                _buildPath += $"/{_buildType}";
            }
        }
        
        EditorGUILayout.LabelField("Path : ", _buildPath, PATH_STYLE);
        EditorGUILayout.Space();
        
        // Build Type Select
        var selected = (BUILD_TYPE) EditorGUILayout.EnumPopup("Build Type :", _buildType);
        if (selected != _buildType) {
            ///Common
            _buildType = selected;
            _applicationIdentifier = PlayerSettings.applicationIdentifier;
            _bundleVersion = PlayerSettings.bundleVersion;
            _defineSymbols = string.Empty;

            /// Android
            _bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _useAPKExpansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
            _buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            _buildApkPerCpuArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;
            _androidCreateSymbolsZip = EditorUserBuildSettings.androidCreateSymbolsZip;
            _exportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;

            /// iOS
            _buildNumber = PlayerSettings.iOS.buildNumber;
            _appleDeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID;
            _iOSManualProvisioningProfileType = PlayerSettings.iOS.iOSManualProvisioningProfileType;
            _iOSManualProvisioningProfileID = PlayerSettings.iOS.iOSManualProvisioningProfileID;
        }

        if (_buildType != default) {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
        } else {
            return;
        }
        
        switch(_buildType) {
            case BUILD_TYPE.ANDROID:
                DrawAndroidOption();
                break;
            case BUILD_TYPE.IOS:
                DrawIosOption();
                break;
            default: 
                return;
        }
        
        if (GUILayout.Button("BUILD", GUILayout.Width(150f), GUILayout.Height(50f))) {
            Build();
        }
        
        if (_buildType != default) {
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawCommonOption() {
		EditorGUILayout.LabelField("applicationIdentifier : ", _applicationIdentifier);

		_bundleVersion = EditorGUILayout.TextField("bundleVersion : ", _bundleVersion);
        _stackTraceLogType = (StackTraceLogType) EditorGUILayout.EnumPopup("StackTraceLogType :", _stackTraceLogType);

        var defineList = EditorConstants.Build.DEFINE_SYMBOLS;
        var selected = new bool[defineList.Length];
        for (var i = 0; i < defineList.Length; ++i) {
            selected[i] = _defineSymbols.Contains(defineList[i]);
        }

        for (var i = 0; i < defineList.Length; ++i) {
            if (string.IsNullOrEmpty(defineList[i])) {
                EditorGUILayout.Space();
            } else if (defineList[i].Contains("==")) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(defineList[i]);
            } else {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField(defineList[i], GUILayout.Width(200));
                    selected[i] = EditorGUILayout.Toggle(selected[i]);
                }

                switch (selected[i]) {
                    case true when _defineSymbols.Contains(defineList[i]) == false: {
                        if (_defineSymbols.Length > 0) {
                            _defineSymbols += ";";
                        }

                        _defineSymbols += defineList[i];
                        break;
                    }
                    case false when _defineSymbols.Contains(defineList[i]): {
                        _defineSymbols = _defineSymbols.Replace(";" + defineList[i], "");
                        _defineSymbols = _defineSymbols.Replace(defineList[i], "");
                        _defineSymbols = _defineSymbols.Replace(";;", ";");
                        if (_defineSymbols.Length > 0 && _defineSymbols[0] == ';') {
                            _defineSymbols = _defineSymbols.Remove(0, 1);
                        }
                        break;
                    }
                }
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        using(new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.TextField("DefineSymbols", BOLD_STYLE, GUILayout.Width(150));
            EditorGUILayout.TextField(_defineSymbols, BOLD_STYLE);
        }
	}

    private void DrawAndroidOption() {
        DrawCommonOption();
        DrawSpace(3);

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Bundle Version Code", GUILayout.Width(200f));
            _bundleVersionCode = Convert.ToInt32(EditorGUILayout.TextField(_bundleVersionCode.ToString()));
        }

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("useAPKExpansionFiles", GUILayout.Width(200f));
            _useAPKExpansionFiles = EditorGUILayout.Toggle(_useAPKExpansionFiles);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("buildAppBundle", GUILayout.Width(200f));
            _buildAppBundle = EditorGUILayout.Toggle(_buildAppBundle);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("buildApkPerCpuArchitecture", GUILayout.Width(200f));
            _buildApkPerCpuArchitecture = EditorGUILayout.Toggle(_buildApkPerCpuArchitecture);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("androidCreateSymbolsZip", GUILayout.Width(200f));
            _androidCreateSymbolsZip = EditorGUILayout.Toggle(_androidCreateSymbolsZip);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("exportAsGoogleAndroidProject", GUILayout.Width(200f));
            _exportAsGoogleAndroidProject = EditorGUILayout.Toggle(_exportAsGoogleAndroidProject);
        }
    }

    private void DrawIosOption() {
        DrawCommonOption();
        DrawSpace(3);

        _buildNumber = EditorGUILayout.TextField("Bundle Version Code", _buildNumber);

        _appleDeveloperTeamID = EditorGUILayout.TextField("appleDeveloperTeamID", _appleDeveloperTeamID);
        _iOSManualProvisioningProfileType = (ProvisioningProfileType)EditorGUILayout.EnumPopup("iOSManualProvisioningProfileType", _iOSManualProvisioningProfileType);
        _iOSManualProvisioningProfileID = EditorGUILayout.TextField("iOSManualProvisioningProfileID", _iOSManualProvisioningProfileID);
    }

    private void DrawSpace(int count) {
        for (int i = 0; i < count; i++) {
            EditorGUILayout.Space();
        }
    }
    
    private void Build() {
        // Common
        var json = new JObject {
            { "buildType", _buildType.ToString() },
            { "applicationIdentifier", _applicationIdentifier},
            { "bundleVersion", _bundleVersion },
            { "defineSymbols", _defineSymbols }
        };
        
        switch (_buildType) {
            case BUILD_TYPE.ANDROID:
                json.Add("bundleVersionCode", _bundleVersionCode);
                json.Add("useAPKExpansionFiles", _useAPKExpansionFiles);
                json.Add("buildAppBundle", _buildAppBundle);
                json.Add("buildApkPerCpuArchitecture", _buildApkPerCpuArchitecture);
                json.Add("androidCreateSymbolsZip", _androidCreateSymbolsZip);
                json.Add("exportAsGoogleAndroidProject", _exportAsGoogleAndroidProject);
                break;
            case BUILD_TYPE.IOS:
                json.Add("buildNumber", _buildNumber);
                json.Add("appleDeveloperTeamID", _appleDeveloperTeamID);
                json.Add("iOSManualProvisioningProfileType", _iOSManualProvisioningProfileType.ToString());
                json.Add("iOSManualProvisioningProfileID", _iOSManualProvisioningProfileID);
                break;
        }
        
        BuildSettings.Instance.SetBuildSettings(json);
        
        var buildOptions = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray()
        };

        if (string.IsNullOrEmpty(_buildPath)) {
            _buildPath = $"Build/{_buildType}";
        }
        
        if (Directory.Exists(_buildPath) == false) {
            if (EditorUtility.DisplayDialog("Folder Not Exists", $"Create Folder ?\nContinue Build Progress\n\n{_buildPath}", "YES", "NO")) {
                Directory.CreateDirectory(_buildPath);
                buildOptions.locationPathName = _buildPath;
                Build(buildOptions);
                return;
            }
            return;
        }

        buildOptions.locationPathName = _buildPath;
        Build(buildOptions);
    }

    private void Build(BuildPlayerOptions buildOptions) {
        buildOptions.target = _buildType.GetBuildTarget();
    }
}

public enum BUILD_TYPE {
    NONE,
    ANDROID,
    IOS
}

public static class BuildTypeExtension {
    public static BuildTarget GetBuildTarget(this BUILD_TYPE type) {
        switch (type) {
            case BUILD_TYPE.ANDROID:
                return BuildTarget.Android;
            case BUILD_TYPE.IOS:
                return BuildTarget.iOS;
            default:
                throw new BuildFailedException($"Invalid Build Type || {type}");        
        }
    }
}
