using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Obsolete]
public class BuildManager_Obsolete : IPostprocessBuildWithReport {

    private static Builder_Obsolete _builder;
    
    public int callbackOrder => 1000;

    private static string _unityProjectPath;
    public static string UNITY_PROJECT_PATH => string.IsNullOrEmpty(_unityProjectPath) ? _unityProjectPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityProjectPath;
    
    public static Builder_Obsolete CreateBuilder(Enum buildType) {
        var builderType = ReflectionProvider.GetSubClassTypes<Builder_Obsolete>()?.Where(x => x.GetCustomAttribute<BuilderAttribute>()?.buildType.Equals(buildType) ?? false).FirstOrDefault();
        if (builderType != null && Activator.CreateInstance(builderType) is Builder_Obsolete builder) {
            _builder = builder;
            return _builder;
        }

        Debug.LogError($"{nameof(buildType)} is Missing enum type. Implement {nameof(Builder_Obsolete)} and {nameof(BuilderAttribute)}");
        return null;
    }

    public static bool TryCreateBuilder(Enum buildType, out Builder_Obsolete builder) {
        _builder = builder = CreateBuilder(buildType);
        return _builder != null;
    }

    public void OnPostprocessBuild(BuildReport report) {
        if (_builder == null) {
            Debug.LogError($"{nameof(_builder)} is Null");
            return;
        }

        try {
            _builder.PostProcess();
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        } finally {
            _builder = null;
        }
    }

    // TODO. 외부 접근 인터페이스로 추출
    public static void BuildOnCLI() {
        try {
            Debug.Log($"=============== Start {nameof(BuildOnCLI)} ===============");
            BuildSettings_Obsolete.SetBuildSettingsOnCLI();
            if (BuildSettings_Obsolete.Instance.TryGetValue<string>("buildType", out var typeText)) {
                var enumType = ReflectionProvider.GetAttributeEnumTypes<BuildTypeEnumAttribute>().First()?.GetEnumValues()?.GetValue(0)?.GetType();
                if (Enum.TryParse(enumType, typeText, out var typeObject) && typeObject is Enum buildType) {
                    _builder = CreateBuilder(buildType);
                    
                    var buildOptions = new BuildPlayerOptions {
                        scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray()
                    };
                    
                    if (BuildSettings_Obsolete.Instance.TryGetValue<string>("buildPath", out var buildPath) == false) {
                        buildPath = $"{UNITY_PROJECT_PATH}/Build/{buildType}";
                    }
                    
                    if (Directory.Exists(buildPath) == false) {
                        Directory.CreateDirectory(buildPath);
                    }
                    
                    buildOptions.locationPathName = buildPath;
                    Debug.Log($"Build Path : {buildOptions.locationPathName}");
                    
                    _builder.StartBuild(buildOptions);
                }
            }
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }
}

[Obsolete]
[RequiresAttributeImplementation(typeof(BuilderAttribute))]
public abstract class Builder_Obsolete {

    private int _countNum = 0;
    private int CountNum => ++_countNum;
    protected string BuildCount => $"[{GetType().Name}] ({CountNum})";

    protected BuildPlayerOptions buildOptions;
    
    protected string BuildPath => buildOptions.locationPathName;
    protected string BuildParentPath => Directory.GetParent(buildOptions.locationPathName)?.FullName;
    
    protected string BuildExecutePath => $"{Constants.Path.PROJECT_PATH}/{BUILD_EXECUTE_FOLDER}/{GetType()?.GetCustomAttribute<BuilderAttribute>()?.buildType.ToString()}";

    protected const string BUILD_EXECUTE_FOLDER = "BuildExecute";

    public virtual void StartBuild(BuildPlayerOptions buildOptions) {
        this.buildOptions = buildOptions;
        if (GetType().TryGetCustomAttribute<BuilderAttribute>(out var attribute)) {
            this.buildOptions.target = attribute.buildTarget;
            this.buildOptions.targetGroup = attribute.buildTargetGroup;
        } else {
            Debug.LogWarning($"{nameof(attribute)} is null. Checking {nameof(BuilderAttribute)}");  
        }

        PreProcess();
        AssetDatabase.Refresh();

        BuildPipeline.BuildPlayer(this.buildOptions);
    }

    protected void PreProcess() {
        Debug.Log($"{BuildCount} - {nameof(PreProcess)}");
        CommonPreProcess();
        OnPreProcess();
    }

    public void PostProcess() {
        Debug.Log($"{BuildCount} - {nameof(PostProcess)}");
        OnPostProcess();

        if (BuildSettings_Obsolete.Instance.TryGetValue<bool>("cleanBurstDebug", out var isClean) && isClean) {
            ClearBurstDebug(BuildParentPath);
        }

        if (BuildSettings_Obsolete.Instance.TryGetValue<bool>("cleanIL2CPPSludge", out isClean) && isClean) {
            ClearIL2CPPSludge(BuildParentPath);
        }

        if (BuildSettings_Obsolete.Instance.TryGetValue<bool>("revealInFinder", out var isShow) && isShow) {
            EditorUtility.RevealInFinder(BuildPath);
        }
    }
    
    private void CommonPreProcess() { }
    
    protected abstract void OnPreProcess();
    protected abstract void OnPostProcess();

    #region [Common]
    
    protected void SetApplicationId(string appId) {
        if (string.IsNullOrEmpty(appId)) {
            Debug.LogError($"{nameof(appId)} is Null or Empty.");
            return;
        }

        PlayerSettings.applicationIdentifier = appId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.applicationIdentifier)} || {PlayerSettings.applicationIdentifier}");
    }

    protected void SetApplicationIdentifier(BuildTargetGroup targetGroup, string identifier) {
        PlayerSettings.SetApplicationIdentifier(targetGroup, identifier);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.SetApplicationIdentifier)} || {PlayerSettings.GetApplicationIdentifier(targetGroup)}");
    }

    protected void SetVersionName(string version) {
        if (string.IsNullOrEmpty(version)) {
            Debug.LogError($"{nameof(version)} is Null or Empty");
            return;
        }

        if (version.Split('.').Length < 3 || PlayerSettings.bundleVersion.Split('.').Length < 3) {
            Debug.LogError($"Invalid {nameof(version)} || {version}");
            return;
        }

        PlayerSettings.bundleVersion = version;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.bundleVersion)} || {PlayerSettings.bundleVersion}");
    }

    protected void SetDefineSymbols(BuildTargetGroup targetGroup, string symbols) {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.SetScriptingDefineSymbols)} || {PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup)}");
    }
    
    protected void SetDevelopmentBuild(bool isActive) {
        EditorUserBuildSettings.development = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.development)} || {EditorUserBuildSettings.development}");
    }

    protected void SetAutoConnectProfile(bool isActive) {
        EditorUserBuildSettings.connectProfiler = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.connectProfiler)} || {EditorUserBuildSettings.connectProfiler}");
    }

    protected void SetDeepProfilingSupport(bool isActive) {
        EditorUserBuildSettings.buildWithDeepProfilingSupport = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.buildWithDeepProfilingSupport)} || {EditorUserBuildSettings.buildWithDeepProfilingSupport}");
    }

    protected void SetScriptDebugging(bool isActive) {
        EditorUserBuildSettings.allowDebugging = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.allowDebugging)} || {EditorUserBuildSettings.allowDebugging}");
    }

    protected void SetScriptBackend(BuildTargetGroup targetGroup, ScriptingImplementation backend) {
        PlayerSettings.SetScriptingBackend(targetGroup, backend);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetScriptingBackend)} || {PlayerSettings.GetScriptingBackend(targetGroup)}");
    }

    protected void SetManagedStrippingLevel(BuildTargetGroup targetGroup, ManagedStrippingLevel strippingLevel) {
        PlayerSettings.SetManagedStrippingLevel(targetGroup, strippingLevel);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetManagedStrippingLevel)} || {PlayerSettings.GetManagedStrippingLevel(targetGroup)}");
    }

    // NET_Unity_4_8 = .NET Framework
    // NET_Standard_2_0 = .NET Standard 2.1
    protected void SetApiCompatibilityLevel(BuildTargetGroup targetGroup, ApiCompatibilityLevel apiLevel) {
        PlayerSettings.SetApiCompatibilityLevel(targetGroup, apiLevel);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetApiCompatibilityLevel)} || {PlayerSettings.GetApiCompatibilityLevel(targetGroup)}");
    }

    protected void SetConditionBuildOptions(ref BuildPlayerOptions buildPlayerOptions, bool isCondition, BuildOptions type) {
        if (isCondition) {
            buildPlayerOptions.options |= type;
        } else {
            buildPlayerOptions.options &= ~type;
        }
        Debug.Log($"{BuildCount} - {type.ToString()} || {isCondition}");
    }

    protected void SetSubTarget(ref BuildPlayerOptions buildPlayerOptions, StandaloneBuildSubtarget subtarget) => SetSubTarget(ref buildPlayerOptions, (int)subtarget);

    protected void SetSubTarget(ref BuildPlayerOptions buildPlayerOptions, int subTarget) {
        buildPlayerOptions.subtarget = subTarget;
        Debug.Log($"{BuildCount} - {nameof(buildPlayerOptions.subtarget)} || {subTarget}");
    }

    #endregion

    #region [Android]

    protected void SetVersionCode(int code) {
        PlayerSettings.Android.bundleVersionCode = code;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.bundleVersionCode)} || {PlayerSettings.Android.bundleVersionCode}");
    }
    
    protected void SetKeystoreName(string name) {
        PlayerSettings.Android.keystoreName = name;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keystoreName)} || {PlayerSettings.Android.keystoreName}");
    }

    protected void SetKeystorePass(string pass) {
        PlayerSettings.Android.keystorePass = pass;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasPass)} || ****");
    }

    protected void SetKeyAliasName(string name) {
        PlayerSettings.Android.keyaliasName = name;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasName)} || {PlayerSettings.Android.keyaliasName}");
    }

    protected void SetKeyAliasPass(string pass) {
        PlayerSettings.Android.keyaliasPass = pass;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasPass)} || ****");
    }

    protected void SetAAB(bool isActive) {
        EditorUserBuildSettings.buildAppBundle = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.buildAppBundle)} || {EditorUserBuildSettings.buildAppBundle}");
    }

    protected void SetBuildApkPerCpuArchitecture(bool isActive) {
        PlayerSettings.Android.buildApkPerCpuArchitecture = isActive;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.buildApkPerCpuArchitecture)} || {PlayerSettings.Android.buildApkPerCpuArchitecture}");
    }

    protected void SetExportAsGoogleAndroidProject(bool isActive) {
        EditorUserBuildSettings.exportAsGoogleAndroidProject = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.exportAsGoogleAndroidProject)} || {EditorUserBuildSettings.exportAsGoogleAndroidProject}");
    }

#if UNITY_2021_1_OR_NEWER
    protected void SetAndroidCreateSymbol(AndroidCreateSymbols symbol) {
        EditorUserBuildSettings.androidCreateSymbols = symbol;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.androidCreateSymbols)} || {EditorUserBuildSettings.androidCreateSymbols}");
    }
#else
    protected void SetAndroidCreateSymbol(bool isActive) {
        EditorUserBuildSettings.androidCreateSymbolsZip = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.androidCreateSymbolsZip)} || {EditorUserBuildSettings.androidCreateSymbolsZip}");
    }
#endif

    #endregion
    
    #region [iOS]

    protected void SetBuildNumber(string buildNumber) {
        PlayerSettings.iOS.buildNumber = buildNumber;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.buildNumber)} || {PlayerSettings.iOS.buildNumber}");
    }

    protected void SetTargetOSVersion(string targetVersion) {
        PlayerSettings.iOS.targetOSVersionString = targetVersion;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.targetOSVersionString)} || {PlayerSettings.iOS.targetOSVersionString}");
    }

    protected void SetAppleEnableAutomaticSigning(bool isAutomatic) {
        PlayerSettings.iOS.appleEnableAutomaticSigning = isAutomatic;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.appleEnableAutomaticSigning)} || {PlayerSettings.iOS.appleEnableAutomaticSigning}");
    }

    protected void SetAppleDeveloperTeamId(string teamId) {
        PlayerSettings.iOS.appleDeveloperTeamID = teamId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.appleDeveloperTeamID)} || {PlayerSettings.iOS.appleDeveloperTeamID}");
    }

    protected void SetProvisioningProfileType(ProvisioningProfileType type) {
        PlayerSettings.iOS.iOSManualProvisioningProfileType = type;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.iOSManualProvisioningProfileType)} || {PlayerSettings.iOS.iOSManualProvisioningProfileType}");
    }

    protected void SetiOSManualProvisioningProfileID(string provisioningId) {
        PlayerSettings.iOS.iOSManualProvisioningProfileID = provisioningId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.iOSManualProvisioningProfileID)} || {PlayerSettings.iOS.iOSManualProvisioningProfileID}");
    }

    protected void SetHideHomeButton(bool isHide) {
        PlayerSettings.iOS.hideHomeButton = isHide;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.hideHomeButton)} || {PlayerSettings.iOS.hideHomeButton}");
    }

    #endregion

    #region [Utils]
    
    protected void ClearBurstDebug(string path) {
        foreach (var directoryPath in Directory.GetDirectories(Path.GetFullPath(path))) {
            if (directoryPath.Contains("DoNotShip")) {
                SystemUtil.DeleteDirectory(directoryPath);
            } else {
                ClearBurstDebug(directoryPath);
            } 
        }
    }

    protected void ClearIL2CPPSludge(string path) {
        foreach (var directoryPath in Directory.GetDirectories(Path.GetFullPath(path))) {
            if (directoryPath.Contains("ButDontShipItWithYourGame")) {
                SystemUtil.DeleteDirectory(directoryPath);
            } else {
                ClearIL2CPPSludge(directoryPath);
            }
        }
    }

    #endregion
}

//******** Sample Enum Attribute Example ********//
/*
[BuildType]
public enum BUILD_TYPE {
    NONE,
    ANDROID,
    IOS
}

[BuildOption(BuildTargetGroup.Unknown)]
public enum COMMON_BUILD_OPTION_TYPE {
    [EnumValue("\n\n=== Common Build Option ===")]
    IL2Cpp,
}

[BuildOption(BuildTargetGroup.Android)]
public enum ANDROID_BUILD_OPTION_TYPE {
    [EnumValue("\n\n=== Android Build Option ===")]
    DEBUG_ANDROID,
}

[BuildOption(BuildTargetGroup.iOS)]
public enum IOS_BUILD_OPTION_TYPE {
    [EnumValue("\n\n=== iOS Build Option ===")]
    DEBUG_IOS,
}

[DefineSymbol]
public enum BUILD_DEFINE_SYMBOL_TYPE {
    [EnumValue("\n\n=== DEBUG ===")]
    DEBUG_LOG
}
*/
