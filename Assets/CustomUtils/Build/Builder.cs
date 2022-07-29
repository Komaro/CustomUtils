using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// TODO. Fix Generic Class
public abstract class Builder : IPreprocessBuildWithReport, IPostprocessBuildWithReport {

    private int countNum = 0;
    private int CountNum => ++countNum;
    protected string BuildCount => $"[{GetType().Name}] ({CountNum})";
    
    public int callbackOrder { get; }

    protected static readonly string DEFAULT_BUILD_ROOT_PATH = "Build/";
    
    public void OnPreprocessBuild(BuildReport report) {
        Debug.Log($"{BuildCount} - Preprocess");
        CommonPreProcess();
        PreProcess();
    }
    
    public void OnPostprocessBuild(BuildReport report) {
        Debug.Log($"{BuildCount} - Postprocess");
        PostProcess();
        EditorUtility.RevealInFinder(DEFAULT_BUILD_ROOT_PATH);
    }

    public void StartBuild(BuildPlayerOptions buildOptions) => BuildPipeline.BuildPlayer(buildOptions);

    private void CommonPreProcess() {
        SetApplicationId(BuildSettings.Instance.GetValue<string>("applicationIdentifier"));
        SetVersionName(BuildSettings.Instance.GetValue<string>("bundleVersion"));
    }

    public abstract void PreProcess();
    public abstract void PostProcess();

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

    #endregion
}