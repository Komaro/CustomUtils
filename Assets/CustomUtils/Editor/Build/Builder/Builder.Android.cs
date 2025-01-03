using Unity.Android.Types;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public partial class Builder {
    
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
}
