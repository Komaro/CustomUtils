using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class EditorBuild : EditorWindow {
    
    #region [Common]
    private string _applicationIdentifier;
    private string _bundleVersion;
    private string _defineSymbols;
    private StackTraceLogType _stackTraceLogType;
    
    private bool _cleanBurstDebug;
    private bool _revealInFinder;
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
    
    private BuilderAttribute _builderAttribute;
    private Vector2 _scrollPos;
    
    private string _buildPath;
    private string _unityPath;
    private string UNITY_PATH => string.IsNullOrEmpty(_unityPath) ? _unityPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityPath;
    
    
    private static Enum _buildTypeDefault;
    private static Enum _buildType;
    private static Dictionary<Enum, DefineSymbolValueAttribute> _defineSymbolDic = new Dictionary<Enum, DefineSymbolValueAttribute>();
    
    private static EditorBuild _window;
    public static EditorBuild window => _window == null ? _window = GetWindow<EditorBuild>("Build") : _window;

    private static readonly GUIStyle DIVIDE_STYLE = new();
    private static readonly GUIStyle PATH_STYLE = new();
    private static readonly GUIStyle BOLD_STYLE = new();
    
    private static readonly GUILayoutOption DEFAULT_LAYOUT = GUILayout.Width(300f);
    
    private static readonly Regex newLineRegex = new Regex(@"(\n)");

    [MenuItem("Build/Open Build Setting")]
    private static void OpenWindow() {
        DIVIDE_STYLE.alignment = TextAnchor.MiddleCenter;
        DIVIDE_STYLE.normal.textColor = Color.cyan;
        
        PATH_STYLE.normal.textColor = Color.white;
        PATH_STYLE.fontStyle = FontStyle.Bold;
        PATH_STYLE.fontSize = 12;
        
        BOLD_STYLE.normal.textColor = Color.gray;
        BOLD_STYLE.fontStyle = FontStyle.Bold;

        if (ReflectionManager.GetAttributeEnums<BuildTypeAttribute>().TryFirst(out var type)) {
            _buildType = _buildTypeDefault = (Enum) type.GetEnumValues()?.GetValue(0);
        } else {
            ShowErrorDialogue("Missing Attribute !!!", $"{nameof(BuildTypeAttribute)} is Missing.\nPlease Implement Enum and Add {nameof(BuildTypeAttribute)}", "OK");
            return;
        }

        _defineSymbolDic.Clear();
        var defineSymbolType = ReflectionManager.GetAttributeEnums<DefineSymbolAttribute>().FirstOrDefault();
        if (defineSymbolType != null) {
            var typeValues = Enum.GetValues(defineSymbolType);
            foreach (var value in typeValues) {
                if (value is Enum enumType) {
                    _defineSymbolDic.Add(enumType, defineSymbolType.GetField(value.ToString()).GetCustomAttribute<DefineSymbolValueAttribute>());
                }
            }
        } else {
            Logger.Warning($"{nameof(DefineSymbolAttribute)} is Missing.");  
        }
        
        window.position = new Rect(800f, 200f, 600f, 800f);
        window.Show();
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("==================== Build Setting ====================", DIVIDE_STYLE);
        if (GUILayout.Button("Clear Build Path", GUILayout.Width(150f), GUILayout.Height(20f))) {
            _buildPath = string.Empty;
        }
        
        if (GUILayout.Button("Select Build Path", GUILayout.Width(150f), GUILayout.Height(20f)) && IsDefaultBuildType() == false) {
            var beforePath = _buildPath;
            _buildPath = EditorUtility.OpenFolderPanel("Build Path", _buildPath, _buildPath);
            if (string.IsNullOrEmpty(_buildPath)) {
                _buildPath = beforePath;
            }
            
            if (string.IsNullOrEmpty(_buildPath)) {
                _buildPath = $"{UNITY_PATH}/Build";
            }
            
            FixBuildPath();
        }
        
        EditorGUILayout.LabelField("Path : ", _buildPath, PATH_STYLE);
        EditorGUILayout.Space();
        
        // Build Type Select
        var selectBuildType = EditorGUILayout.EnumPopup("Build Type :", _buildType ?? _buildTypeDefault);
        if (_buildType?.Equals(selectBuildType) == false) {
            _buildType = selectBuildType;
            FixBuildPath();
            
            // Common
            _applicationIdentifier = PlayerSettings.applicationIdentifier;
            _bundleVersion = PlayerSettings.bundleVersion;
            _defineSymbols = string.Empty;

            // Android
            _bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _useAPKExpansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
            _buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            _buildApkPerCpuArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;
            _androidCreateSymbolsZip = EditorUserBuildSettings.androidCreateSymbolsZip;
            _exportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;

            // iOS
            _buildNumber = PlayerSettings.iOS.buildNumber;
            _appleDeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID;
            _iOSManualProvisioningProfileType = PlayerSettings.iOS.iOSManualProvisioningProfileType;
            _iOSManualProvisioningProfileID = PlayerSettings.iOS.iOSManualProvisioningProfileID;
        }
        
        if (IsDefaultBuildType() == false) {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
        } else {
            return;
        }
        
        if (_builderAttribute == null || _builderAttribute.buildType.Equals(_buildType) == false) {
            if (TryGetBuilderAttribute(out _builderAttribute) == false) {
                Debug.LogError($"{nameof(_builderAttribute)} is Null. Create {_buildType} {nameof(Builder)} and {nameof(_builderAttribute)}");
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        
        switch (_builderAttribute.buildTarget) {
            case BuildTarget.Android:
                DrawAndroidOption();
                break;
            case BuildTarget.iOS:
                DrawIosOption();
                break;
            default:
                return;
        }
        
        if (GUILayout.Button("BUILD", GUILayout.Width(150f), GUILayout.Height(50f))) {
            Build();
        }

        if (IsDefaultBuildType() == false) {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawCommonOption() {
		EditorGUILayout.LabelField("applicationIdentifier : ", _applicationIdentifier);

		_bundleVersion = EditorGUILayout.TextField("bundleVersion : ", _bundleVersion);
        _stackTraceLogType = (StackTraceLogType) EditorGUILayout.EnumPopup("StackTraceLogType :", _stackTraceLogType);
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Clean Burst Debug", DEFAULT_LAYOUT);
            _cleanBurstDebug = EditorGUILayout.Toggle(_cleanBurstDebug);
        }

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Reveal In Finder", DEFAULT_LAYOUT);
            _revealInFinder = EditorGUILayout.Toggle(_revealInFinder);
        }

        var defineList = _defineSymbolDic.Keys.ToList();
        var selected = new bool[defineList.Count];
        for (var i = 0; i < defineList.Count; i++) {
            selected[i] = _defineSymbols.Contains(defineList[i].ToString());
        }

        for (var i = 0; i < defineList.Count; i++) {
            var key = defineList[i];
            var attribute = _defineSymbolDic[key];
            if (attribute != null) {
                var match = newLineRegex.Split(attribute.divideText);
                if (match.Length > 0) {
                    match.ForEach(x => {
                        if (newLineRegex.IsMatch(x)) {
                            EditorGUILayout.Space();
                        } else {
                            if (string.IsNullOrEmpty(x) == false) {
                                EditorGUILayout.LabelField(x, DEFAULT_LAYOUT);
                            }
                        }
                    });
                }
            }
            
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(key.ToString(), DEFAULT_LAYOUT);
                selected[i] = EditorGUILayout.Toggle(selected[i]);
            }
            
            switch (selected[i]) {
                case true when _defineSymbols.Contains(defineList[i].ToString()) == false:
                    if (_defineSymbols.Length > 0) {
                        _defineSymbols += ";";
                    }

                    _defineSymbols += defineList[i];
                    break;
                case false when _defineSymbols.Contains(defineList[i].ToString()):
                    _defineSymbols = _defineSymbols.Replace(";" + defineList[i], "");
                    _defineSymbols = _defineSymbols.Replace(defineList[i].ToString(), "");
                    _defineSymbols = _defineSymbols.Replace(";;", ";");
                    if (_defineSymbols.Length > 0 && _defineSymbols[0] == ';') {
                        _defineSymbols = _defineSymbols.Remove(0, 1);
                    }
                    break;
            }
        }
        
        DrawSpace(3);

        using(new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.TextField("DefineSymbols", BOLD_STYLE, GUILayout.Width(150));
            EditorGUILayout.TextField(_defineSymbols, BOLD_STYLE);
        }
	}

    private void DrawAndroidOption() {
        DrawCommonOption();
        DrawSpace(3);

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Bundle Version Code", DEFAULT_LAYOUT);
            _bundleVersionCode = Convert.ToInt32(EditorGUILayout.TextField(_bundleVersionCode.ToString()));
        }

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("useAPKExpansionFiles", DEFAULT_LAYOUT);
            _useAPKExpansionFiles = EditorGUILayout.Toggle(_useAPKExpansionFiles);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("buildAppBundle", DEFAULT_LAYOUT);
            _buildAppBundle = EditorGUILayout.Toggle(_buildAppBundle);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("buildApkPerCpuArchitecture", DEFAULT_LAYOUT);
            _buildApkPerCpuArchitecture = EditorGUILayout.Toggle(_buildApkPerCpuArchitecture);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("androidCreateSymbolsZip", DEFAULT_LAYOUT);
            _androidCreateSymbolsZip = EditorGUILayout.Toggle(_androidCreateSymbolsZip);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("exportAsGoogleAndroidProject", DEFAULT_LAYOUT);
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
        var json = new JObject {
            // Common
            { "buildType", _buildType.ToString() },
            { "applicationIdentifier", _applicationIdentifier},
            { "bundleVersion", _bundleVersion },
            { "defineSymbols", _defineSymbols },
            { "cleanBurstDebug", _cleanBurstDebug },
            { "revealInFinder", _revealInFinder}
        };

        if (_builderAttribute != null) {
            switch (_builderAttribute.buildTarget) {
                case BuildTarget.Android:
                    json.Add("bundleVersionCode", _bundleVersionCode);
                    json.Add("useAPKExpansionFiles", _useAPKExpansionFiles);
                    json.Add("buildAppBundle", _buildAppBundle);
                    json.Add("buildApkPerCpuArchitecture", _buildApkPerCpuArchitecture);
                    json.Add("androidCreateSymbolsZip", _androidCreateSymbolsZip);
                    json.Add("exportAsGoogleAndroidProject", _exportAsGoogleAndroidProject);
                    break;
                case BuildTarget.iOS:
                    json.Add("buildNumber", _buildNumber);
                    json.Add("appleDeveloperTeamID", _appleDeveloperTeamID);
                    json.Add("iOSManualProvisioningProfileType", _iOSManualProvisioningProfileType.ToString());
                    json.Add("iOSManualProvisioningProfileID", _iOSManualProvisioningProfileID);
                    break;
            }
        }
        
        BuildSettings.Instance.SetBuildSettings(json);
        
        var buildOptions = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray()
        };

        if (string.IsNullOrEmpty(_buildPath)) {
            _buildPath = $"Build/{_buildType}";
        }
        
        if (Directory.Exists(_buildPath) == false) {
            if (ShowDialogue("Folder Not Exists", $"Create Folder ?\nContinue Build Progress\n\n{_buildPath}", "YES", "NO")) {
                Directory.CreateDirectory(_buildPath);
                buildOptions.locationPathName = _buildPath;
                Build(buildOptions);
            }
            return;
        }

        buildOptions.locationPathName = _buildPath;
        Build(buildOptions);
    }

    private void Build(BuildPlayerOptions buildOptions) {
        if (_builderAttribute != null && BuildManager.TryCreateBuilder(_builderAttribute.buildType, out var builder)) {
            builder?.StartBuild(buildOptions);
        } else {
            Debug.LogError($"{nameof(_builderAttribute)} is Null");
        }
    }

    private bool TryGetBuilderAttribute(out BuilderAttribute attribute) {
        attribute = GetBuilderAttribute();
        return attribute != null;
    }

    private BuilderAttribute GetBuilderAttribute() {
        var attributeList = ReflectionManager.GetSubClassTypes<Builder>()?.Where(x => x.GetCustomAttribute<BuilderAttribute>()?.buildType.Equals(_buildType) ?? false).ToList();
        if (attributeList is not { Count: > 0 }) {
            return null;
        }
        
        if (attributeList.Count > 1) {
            Debug.LogError($"{nameof(attributeList)} count over. {nameof(BuilderAttribute.buildType)} must not be duplicated. || {attributeList.Count}");
            attributeList.ForEach(x => Debug.LogError($"Duplicate {nameof(Builder)} || {x.FullName}"));
            return null;
        }

        return attributeList.First().GetCustomAttribute<BuilderAttribute>();
    }

    private void FixBuildPath() {
        if (string.IsNullOrEmpty(_buildPath) || IsDefaultBuildType()) {
            _buildPath = string.Empty;
            return;
        }

        var regex = new Regex(@$"\/{_buildType}");
        if (regex.IsMatch(_buildPath) == false) {
            var directory = _buildPath.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(directory) == false && Enum.IsDefined(_buildTypeDefault.GetType(), directory)) {
                _buildPath = _buildPath.Replace($"/{directory}", string.Empty);
            }
            
            _buildPath += $"/{_buildType}";
        }
    }
    
    // TODO. to Module
    private static bool ShowErrorDialogue(string title, string message, string ok = "", string cancel = "") {
        Logger.TraceError(message);
        return ShowDialogue(title, message, ok, cancel);
    }
    
    // TODO. to Module
    private static bool ShowDialogue(string title, string message, string ok = "", string cancel = "") => EditorUtility.DisplayDialog(title, message, ok, cancel);
    
    private bool IsDefaultBuildType(Enum buildType = null) => _buildTypeDefault.Equals(buildType ?? _buildType);
}
