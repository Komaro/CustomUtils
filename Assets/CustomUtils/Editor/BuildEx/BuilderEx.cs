
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[RequiresAttributeImplementation(typeof(BuilderAttribute))]
public abstract partial class BuilderEx : IDisposable {

    protected Enum buildType;
    protected BuildTarget buildTarget;
    protected BuildTargetGroup buildTargetGroup;
    
    private int _countNum = 0;
    private int CountNum => ++_countNum;
    protected string BuildCount => $"[{GetType().Name}.{buildType}] ({CountNum})";

    public BuilderEx() {
        if (GetType().TryGetCustomAttribute<BuilderAttribute>(out var attribute)) {
            buildType = attribute.buildType;
            buildTarget = attribute.buildTarget;
            buildTargetGroup = attribute.buildTargetGroup;
        }
    }

    ~BuilderEx() => Dispose();
    public virtual void Dispose() => GC.SuppressFinalize(this);

    public static bool TryCreateBuilder(Enum buildType, out BuilderEx builder) => (builder = CreateBuilder(buildType)) != null;

    public static BuilderEx CreateBuilder(Enum buildType) {
        foreach (var type in ReflectionProvider.GetSubClassTypes<BuilderEx>()) {
            if (type.TryGetCustomAttribute<BuilderAttribute>(out var attribute) && attribute.buildType.Equals(buildType)) {
                return CreateBuilder(type);
            }
        }

        return null;
    }
    
    public static bool TryCreateBuilder(Type builderType, out BuilderEx builder) => (builder = CreateBuilder(builderType)) != null;
    
    public static BuilderEx CreateBuilder(Type builderType) {
        if (builderType.IsSubclassOf(typeof(BuilderEx))) {
            return SystemUtil.SafeCreateInstance<BuilderEx>(builderType);
        }

        return null;
    }

    public void StartBuild() {
        var options = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray()
        };
    
        if (BuildConfigProvider.TryGetValue<string>(nameof(BuildConfig.buildDirectory), out var buildDirectory) == false || string.IsNullOrEmpty(buildDirectory)) {
            buildDirectory = $"{Constants.Path.BUILD_ROOT_PATH}/{buildType}";
        }

        options.locationPathName = buildDirectory;
        StartBuild(options);
    }

    public virtual void StartBuild(BuildPlayerOptions options) {
        options.target = buildTarget;
        options.targetGroup = buildTargetGroup;

        Debug.Log($"{BuildCount} - Start {nameof(OnPreProcess)}");

        PreProcess(ref options);
        BuildPipeline.BuildPlayer(options);
    }

    protected virtual void PreProcess(ref BuildPlayerOptions options) {
        Debug.Log($"{BuildCount} - Start {nameof(OnPreProcess)}");
        SetDevelopmentBuild(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.developmentBuild)));
        SetAutoConnectProfile(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.autoConnectProfile)));
        SetDeepProfilingSupport(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.deepProfilingSupport)));
        SetScriptDebugging(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.scriptDebugging)));
        
        OnPreProcess(ref options);
        if (BuildConfigProvider.IsTrue(DEFAULT_CUSTOM_BUILD_OPTION.refreshAssetDatabase)) {
            AssetDatabase.Refresh();
        }
    }
    
    protected abstract void OnPreProcess(ref BuildPlayerOptions options);

    public virtual void PostProcess(BuildSummary summary) {
        Debug.Log($"{BuildCount} - Start {nameof(OnPostProcess)}");
        OnPostProcess(summary);

        var optionDic = BuildConfigProvider.GetValue<Dictionary<string, bool>>(nameof(BuildConfig.optionDic));
        var path = Directory.GetParent(summary.outputPath)?.FullName;
        if (string.IsNullOrEmpty(path) == false) {
            if (optionDic.IsTrue(DEFAULT_CUSTOM_BUILD_OPTION.cleanBurstDebug.ToString())) {
                ClearBurstDebug(path);
            }

            if (optionDic.IsTrue(DEFAULT_CUSTOM_BUILD_OPTION.cleanIL2CPPSludge.ToString())) {
                ClearIL2CPPSludge(path);
            }
        }
        
        if (optionDic.IsTrue(DEFAULT_CUSTOM_BUILD_OPTION.revealInFinder.ToString())) {
            EditorUtility.RevealInFinder(summary.outputPath);
        }
    }
    
    protected abstract void OnPostProcess(BuildSummary summary);
    
    
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