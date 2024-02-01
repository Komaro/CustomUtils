using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorBuild : EditorWindow {
    
    #region [Common]
    private string _applicationIdentifier;
    private string _bundleVersion;
    private string _defineSymbols;
    private StackTraceLogType _stackTraceLogType;

    private bool _ignoreResourcesReimport;
    private bool _cleanBurstDebug;
    private bool _cleanIL2CPPSludge;
    private bool _revealInFinder;
    
    private bool _developmentBuild;
    private bool _autoConnectProfile;
    private bool _deepProfilingSupport;
    private bool _scriptDebugging;
    
    
    #endregion

    #region [Android]
    private int _bundleVersionCode;
    private bool _useAPKExpansionFiles;
    private bool _buildAppBundle;
    private bool _buildApkPerCpuArchitecture;
    private bool _exportAsGoogleAndroidProject;
#if UNITY_2021_1_OR_NEWER
    private AndroidCreateSymbols _androidCreateSymbols;
#else
    private bool _androidCreateSymbols;
#endif
    
    #endregion

    #region [iOS]
    private string _buildNumber;
    private string _appleDeveloperTeamID;
    private ProvisioningProfileType _iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
    private string _iOSManualProvisioningProfileID;
    #endregion

    #region [Option]

    private Dictionary<Enum, bool> _optionDic = new();

    #endregion
    
    private BuilderAttribute _builderAttribute;
    private Vector2 _scrollPos;
    
    private string _buildPath;

    private static Enum _buildType;
    private static Enum _selectBuildType;
    private static Dictionary<BuildTargetGroup, Dictionary<Enum, EnumValueAttribute>> _buildOptionDic = new ();
    private static Dictionary<Enum, EnumValueAttribute> _defineSymbolDic = new Dictionary<Enum, EnumValueAttribute>();

    private static EditorBuild _window;
    public static EditorBuild window => _window == null ? _window = GetWindow<EditorBuild>("Build") : _window;

    private static readonly Enum DEFAULT_BUILD_TYPE = (Enum) ReflectionManager.GetAttributeEnumTypes<BuildTypeAttribute>().First()?.GetEnumValues().GetValue(0);
    private static readonly GUIStyle DIVIDE_STYLE = new();
    private static readonly GUIStyle PATH_STYLE = new();
    private static readonly GUIStyle BOLD_STYLE = new();
    
    private static readonly GUILayoutOption DEFAULT_LAYOUT = GUILayout.Width(300f);
    
    private static readonly Regex newLineRegex = new Regex(@"(\n)");

    [MenuItem("Build/Open Build Setting %F1")]
    private static void OpenWindow() {
        DIVIDE_STYLE.alignment = TextAnchor.MiddleCenter;
        DIVIDE_STYLE.normal.textColor = Color.cyan;
        
        PATH_STYLE.normal.textColor = Color.white;
        PATH_STYLE.fontStyle = FontStyle.Bold;
        PATH_STYLE.fontSize = 12;
        
        BOLD_STYLE.normal.textColor = Color.gray;
        BOLD_STYLE.fontStyle = FontStyle.Bold;

        _buildType = DEFAULT_BUILD_TYPE; 

        _buildOptionDic.Clear();
        var enumTypes = ReflectionManager.GetAttributeEnumTypes<BuildOptionAttribute>();
        if (enumTypes != null) {
            foreach (var type in enumTypes) {
                var enumAttribute = type.GetCustomAttribute<BuildOptionAttribute>();
                foreach (var value in Enum.GetValues(type)) {
                    if (value is Enum optionType) {
                        _buildOptionDic.AutoAdd(enumAttribute.buildTargetGroup, optionType, type.GetField(value.ToString()).GetCustomAttribute<EnumValueAttribute>());
                    }
                }
            }
        }
        
        _defineSymbolDic.Clear();
        var enumType = ReflectionManager.GetAttributeEnumTypes<DefineSymbolAttribute>().FirstOrDefault();
        if (enumType != null) {
            foreach (var value in Enum.GetValues(enumType)) {
                if (value is Enum type) {
                    _defineSymbolDic.Add(type, enumType.GetField(value.ToString()).GetCustomAttribute<EnumValueAttribute>());
                }
            }
        }
        
        // window.position = new Rect(800f, 200f, 600f, 800f);
        window.Show();
        window.Focus();
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
                _buildPath = $"{BuildManager.UNITY_PROJECT_PATH}/Build";
            }
            
            FixBuildPath();
        }
        
        EditorGUILayout.LabelField("Path : ", _buildPath, PATH_STYLE);
        EditorGUILayout.Space();
        
        // Build Type Select
        try {
            _buildType ??= DEFAULT_BUILD_TYPE;
            if (_selectBuildType == null || _selectBuildType.Equals(DEFAULT_BUILD_TYPE)) {
                _selectBuildType = EditorGUILayout.EnumPopup("Build Type :", _buildType);
            } else {
                _selectBuildType = EditorGUILayout.EnumPopup("Build Type :", _selectBuildType);
            }
        } catch {
            OpenWindow();
            return;
        }

        if (_buildType.Equals(_selectBuildType) == false) {
            _buildType = _selectBuildType;
            FixBuildPath();
            
            // Common
            _applicationIdentifier = PlayerSettings.applicationIdentifier;
            _bundleVersion = PlayerSettings.bundleVersion;
            _defineSymbols = string.Empty;

            _developmentBuild = EditorUserBuildSettings.development;
            _autoConnectProfile = EditorUserBuildSettings.connectProfiler;
            _deepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            _scriptDebugging = EditorUserBuildSettings.allowDebugging;
            
            // Android
            _bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _useAPKExpansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
            _buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            _buildApkPerCpuArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;
            _exportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
#if UNITY_2021_1_OR_NEWER
            _androidCreateSymbols = EditorUserBuildSettings.androidCreateSymbols;
#else
            EditorUserBuildSettings.androidCreateSymbolsZip = false;
#endif
            // iOS
            _buildNumber = PlayerSettings.iOS.buildNumber;
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawCommonOption() {
		EditorGUILayout.LabelField("applicationIdentifier : ", _applicationIdentifier);

		_bundleVersion = EditorGUILayout.TextField("bundleVersion : ", _bundleVersion);
        _stackTraceLogType = (StackTraceLogType) EditorGUILayout.EnumPopup("StackTraceLogType :", _stackTraceLogType);

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Ignore Resources Reimport", DEFAULT_LAYOUT);
            _ignoreResourcesReimport = EditorGUILayout.Toggle(_ignoreResourcesReimport);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Clean Burst Debug", DEFAULT_LAYOUT);
            _cleanBurstDebug = EditorGUILayout.Toggle(_cleanBurstDebug);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Clean IL2CPP Sludge", DEFAULT_LAYOUT);
            _cleanIL2CPPSludge = EditorGUILayout.Toggle(_cleanIL2CPPSludge);
        }

        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Reveal In Finder", DEFAULT_LAYOUT);
            _revealInFinder = EditorGUILayout.Toggle(_revealInFinder);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("DevelpmentBuild", DEFAULT_LAYOUT);
            _developmentBuild = EditorGUILayout.Toggle(_developmentBuild);
        }

        if (_developmentBuild) {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Auto Connect Profile", DEFAULT_LAYOUT);
                _autoConnectProfile = EditorGUILayout.Toggle(_autoConnectProfile);
            }
            
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Deep Profiling Support", DEFAULT_LAYOUT);
                _deepProfilingSupport = EditorGUILayout.Toggle(_deepProfilingSupport);
            }
            
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Script Debugging", DEFAULT_LAYOUT);
                _scriptDebugging = EditorGUILayout.Toggle(_scriptDebugging);
            }
        }
        
        DrawSpace(3);
        DrawBuildOption();
        DrawSpace(3);

        var defineList = _defineSymbolDic.Keys.ToList();
        var selected = new bool[defineList.Count];
        for (var i = 0; i < defineList.Count; i++) {
            selected[i] = _defineSymbols.Contains(defineList[i].ToString());
        }

        for (var i = 0; i < defineList.Count; i++) {
            var key = defineList[i];
            var attribute = _defineSymbolDic[key];
            if (attribute != null) {
                DrawTextField(attribute.divideText);
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
    
    private void DrawBuildOption() {
        DrawBuildOptionTargetGroup(BuildTargetGroup.Unknown);
        DrawBuildOptionTargetGroup(_builderAttribute.buildTargetGroup);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawBuildOptionTargetGroup(BuildTargetGroup targetGroup) {
        if (_buildOptionDic.TryGetValue(targetGroup, out var optionDic)) {
            foreach (var option in optionDic) {
                if (option.Value != null) {
                    DrawTextField(option.Value.divideText);
                }
                
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField(option.Key.ToString().Replace('_', ' '), DEFAULT_LAYOUT);
                    if (_optionDic.ContainsKey(option.Key) == false) {
                        _optionDic.Add(option.Key, false);
                    }
                    
                    _optionDic[option.Key] = EditorGUILayout.Toggle(_optionDic[option.Key]);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawTextField(string text) {
        var match = newLineRegex.Split(text);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            EditorGUILayout.LabelField("exportAsGoogleAndroidProject", DEFAULT_LAYOUT);
            _exportAsGoogleAndroidProject = EditorGUILayout.Toggle(_exportAsGoogleAndroidProject);
        }
        
        using (new EditorGUILayout.HorizontalScope()) {
#if UNITY_2021_1_OR_NEWER
            _androidCreateSymbols = (AndroidCreateSymbols) EditorGUILayout.EnumPopup("androidCreateSymbols", _androidCreateSymbols);
#else
            EditorGUILayout.LabelField("androidCreateSymbols", DEFAULT_LAYOUT);
            _androidCreateSymbols = EditorGUILayout.Toggle(_androidCreateSymbols);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawIosOption() {
        DrawCommonOption();
        DrawSpace(3);

        _buildNumber = EditorGUILayout.TextField("Bundle Version Code", _buildNumber);

        _appleDeveloperTeamID = EditorGUILayout.TextField("appleDeveloperTeamID", _appleDeveloperTeamID);
        _iOSManualProvisioningProfileType = (ProvisioningProfileType)EditorGUILayout.EnumPopup("iOSManualProvisioningProfileType", _iOSManualProvisioningProfileType);
        _iOSManualProvisioningProfileID = EditorGUILayout.TextField("iOSManualProvisioningProfileID", _iOSManualProvisioningProfileID);
    }
    
    private void DrawSpace(int count) {
        for (var i = 0; i < count; i++) {
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
            
            { "ignoreResourcesReimport", _ignoreResourcesReimport },
            { "cleanBurstDebug", _cleanBurstDebug },
            { "cleanIL2CPPSludge", _cleanIL2CPPSludge },
            { "revealInFinder", _revealInFinder},
            
            { "developmentBuild", _developmentBuild},
            { "autoConnectProfile", _autoConnectProfile},
            { "deepProfilingSupport", _deepProfilingSupport},
            { "scriptDebugging", _scriptDebugging},
        };

        if (_builderAttribute != null) {
            foreach (var option in _optionDic) {
                json.Add(option.Key.ToString(), option.Value);
            }

            switch (_builderAttribute.buildTarget) {
                case BuildTarget.Android:
                    json.Add("bundleVersionCode", _bundleVersionCode);
                    json.Add("useAPKExpansionFiles", _useAPKExpansionFiles);
                    json.Add("buildAppBundle", _buildAppBundle);
                    json.Add("buildApkPerCpuArchitecture", _buildApkPerCpuArchitecture);
                    json.Add("exportAsGoogleAndroidProject", _exportAsGoogleAndroidProject);
                    json.Add("androidCreateSymbols", _androidCreateSymbols.ToString());
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
            _buildPath = $"{BuildManager.UNITY_PROJECT_PATH}/Build/{_buildType}";
        }
        
        if (Directory.Exists(_buildPath) == false) {
            if (EditorUtility.DisplayDialog("Folder Not Exists", $"Create Folder ?\nContinue Build Progress\n\n{_buildPath}", "YES", "NO")) {
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
        if (_builderAttribute != null) {
            SwitchPlatform();
            if (BuildManager.TryCreateBuilder(_builderAttribute.buildType, out var builder)) {
                builder.StartBuild(buildOptions);
            }
        } else {
            Debug.LogError($"{nameof(_builderAttribute)} is Null");
        }
    }

    private void SwitchPlatform() {
        if (_builderAttribute.buildTarget != EditorUserBuildSettings.activeBuildTarget) {
            EditorUserBuildSettings.SwitchActiveBuildTarget(_builderAttribute.buildTargetGroup, _builderAttribute.buildTarget);
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
            if (string.IsNullOrEmpty(directory) == false && Enum.IsDefined(DEFAULT_BUILD_TYPE.GetType(), directory)) {
                _buildPath = _buildPath.Replace($"/{directory}", string.Empty);
            }
            
            _buildPath += $"/{_buildType}";
        }
    }
    
    private bool IsDefaultBuildType(Enum buildType = null) => DEFAULT_BUILD_TYPE.Equals(buildType ?? _buildType);
}
