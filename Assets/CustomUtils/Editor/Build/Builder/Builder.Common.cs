﻿using UnityEditor;
using UnityEngine;

public partial class Builder {
    
    protected void SetApplicationId(string appId) {
        if (string.IsNullOrEmpty(appId)) {
            Debug.LogError($"{nameof(appId)} is null or empty.");
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
}