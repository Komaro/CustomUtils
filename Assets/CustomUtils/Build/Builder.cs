using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildManager : IPreprocessBuildWithReport, IPostprocessBuildWithReport {

    private static Builder _builder;
    
    public int callbackOrder { get; }

    public static Builder CreateBuilder(Enum buildType) {
        var builderType = ReflectionManager.GetSubClassTypes<Builder>()?.Where(x => x.GetCustomAttribute<BuilderAttribute>()?.buildType.Equals(buildType) ?? false).FirstOrDefault();
        if (builderType != null && Activator.CreateInstance(builderType) is Builder builder) {
            _builder = builder;
            return _builder;
        }

        Debug.LogError($"{nameof(buildType)} is Missing enum type. Implement {nameof(Builder)} and {nameof(BuilderAttribute)}");
        return null;
    }

    public static bool TryCreateBuilder(Enum buildType, out Builder builder) {
        _builder = builder = CreateBuilder(buildType);
        return _builder != null;
    }

    public void OnPreprocessBuild(BuildReport report) {
        if (_builder == null) {
            return;
        }

        try {
            _builder.PreProcess();
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        } finally {
            _builder = null;
        }
    }
    
    public void OnPostprocessBuild(BuildReport report) {
        if (_builder == null) {
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
}

public abstract class Builder {

    private int _countNum = 0;
    private int CountNum => ++_countNum;
    protected string BuildCount => $"[{GetType().Name}] ({CountNum})";
    
    protected static readonly string DEFAULT_BUILD_ROOT_PATH = "Build/";
    
    public virtual void StartBuild(BuildPlayerOptions buildOptions) {
        var attribute = GetType().GetCustomAttribute<BuilderAttribute>();
        if (attribute != null) {
            buildOptions.target = attribute.buildTarget;
        } else {
            Debug.LogWarning($"{nameof(attribute)} is Null. Checking {nameof(BuilderAttribute)}");  
        }
        
        BuildPipeline.BuildPlayer(buildOptions);
    }

    public void PreProcess() {
        Debug.Log($"{BuildCount} - Preprocess");
        CommonPreProcess();
        OnPreProcess();
    }

    public void PostProcess() {
        Debug.Log($"{BuildCount} - Postprocess");
        OnPostProcess();

        if (BuildSettings.Instance.TryGetValue<bool>("cleanBurstDebug", out var isClean) && isClean) {
            ClearBurstDebug(DEFAULT_BUILD_ROOT_PATH);
        }

        if (BuildSettings.Instance.TryGetValue<bool>("revealInFinder", out var isShow) && isShow) {
            EditorUtility.RevealInFinder(DEFAULT_BUILD_ROOT_PATH);
        }
    }

    protected abstract void OnPreProcess();
    protected abstract void OnPostProcess();
    
    private void CommonPreProcess() {
        SetApplicationId(BuildSettings.Instance.GetValue<string>("applicationIdentifier"));
        SetVersionName(BuildSettings.Instance.GetValue<string>("bundleVersion"));
    }
    
    #region [Common]
    
    protected void SetApplicationId(string appId) {
        if (string.IsNullOrEmpty(appId)) {
            Debug.LogError($"{appId} is Null or Empty.");
            return;
        }

        PlayerSettings.applicationIdentifier = appId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.applicationIdentifier)} || {PlayerSettings.applicationIdentifier}");
    }

    protected void SetVersionName(string version) {
        if (string.IsNullOrEmpty(version)) {
            Debug.LogError($"{version} is Null or Empty");
            return;
        }

        if (version.Split('.').Length < 3 || PlayerSettings.bundleVersion.Split('.').Length < 3) {
            Debug.LogError($"Invalid {version} || {version}");
            return;
        }

        PlayerSettings.bundleVersion = version;
        Debug.Log($"{CountNum} - {nameof(PlayerSettings.bundleVersion)} || {PlayerSettings.bundleVersion}");
    }

    protected void SetDefineSymbols(BuildTargetGroup targetGroup, string symbols) {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.SetScriptingDefineSymbols)} || {PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup)}");
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
    
    #endregion
    
    #region [iOS]

    protected void SetBuildNumber(string buildNumber) {
        PlayerSettings.iOS.buildNumber = buildNumber;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.buildNumber)} || {PlayerSettings.iOS.buildNumber}");
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

    protected void ClearBurstDebug(string path) {
        path = Path.GetFullPath(path);
        foreach (var directory in Directory.GetDirectories(path)) {
            if (directory.Contains("DoNotShip")) {
                ClearDirectory(directory);
            } else {
                ClearBurstDebug(directory);    
            }
        }
    }
    
    protected void ClearDirectory(string path) {
        if (Directory.Exists(path)) {
            Debug.Log($"Clear Directory || {path}");
            Directory.Delete(path, true);
        }
    }
}


[AttributeUsage(AttributeTargets.Class)]
public class BuilderAttribute : Attribute {
    
    public Enum buildType;
    public BuildTarget buildTarget;
    
    /// <param name="buildType">enum Value</param>
    /// <param name="buildTarget"></param>
    public BuilderAttribute(object buildType, BuildTarget buildTarget) {
        if (buildType is Enum enumType) {
            this.buildType = enumType;
        } else {
            this.buildType = default;
        }
        
        this.buildTarget = buildTarget;
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class BuildTypeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Enum)]
public class DefineSymbolAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class DefineSymbolValueAttribute : Attribute {
    
    public string divideText;

    public DefineSymbolValueAttribute(string divideText = "") {
        this.divideText = divideText;
    }
}