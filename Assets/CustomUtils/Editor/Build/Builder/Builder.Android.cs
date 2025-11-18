#if UNITY_6000_0_OR_NEWER
using Unity.Android.Types;
using UnityEditor.Android;
#endif

using UnityEditor;
using UnityEngine;

public partial class BuilderBase {
    
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

#if UNITY_6000_0_OR_NEWER
    protected void SetAndroidCreateSymbol(DebugSymbolLevel symbolLevel) {
        UserBuildSettings.DebugSymbols.level = symbolLevel;
        Debug.Log($"{BuildCount} - {nameof(UserBuildSettings.DebugSymbols.level)} || {UserBuildSettings.DebugSymbols.level}");
    }
#elif  UNITY_2021_1_OR_NEWER
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

    protected void SetMinSdkVersion(AndroidSdkVersions versions) {
        PlayerSettings.Android.minSdkVersion = versions;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.minSdkVersion)} || {PlayerSettings.Android.minSdkVersion}");
    }
    
    protected void SetMinSdkVersion(int version) {
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)version;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.minSdkVersion)} || {PlayerSettings.Android.minSdkVersion}");
    }

    protected void SetTargetSdkVersion(AndroidSdkVersions versions) {
        PlayerSettings.Android.targetSdkVersion = versions;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.targetSdkVersion)} || {PlayerSettings.Android.targetSdkVersion}");
    }
    
    protected void SetTargetSdkVersion(int versions) {
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)versions;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.targetSdkVersion)} || {PlayerSettings.Android.targetSdkVersion}");
    }
}
