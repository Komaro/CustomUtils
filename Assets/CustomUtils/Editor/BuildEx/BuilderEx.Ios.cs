
using UnityEditor;
using UnityEngine;

public partial class BuilderEx {
    
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

}
