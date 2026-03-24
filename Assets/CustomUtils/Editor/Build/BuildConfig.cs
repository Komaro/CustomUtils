using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[RequiresAttributeImplementation(typeof(BuildConfigAttribute))]
public abstract partial class BuildConfig : JsonCoroutineAutoConfig {

    public string buildDirectory = string.Empty;
    public readonly Dictionary<string, bool> optionDic = new();

    public string defineSymbols;
    public string applicationIdentifier;
    public string bundleVersion;
    public readonly Dictionary<LogType, StackTraceLogType> stackTraceDic = new();

    public bool developmentBuild;
    public bool autoConnectProfile;
    public bool deepProfilingSupport;
    public bool scriptDebugging;

    public bool isLogBuildReport;

    public BuildConfig() {
        if (GetType().TryGetCustomAttribute<BuildConfigAttribute>(out var targetAttribute)) {
            defineSymbols = targetAttribute.buildTargetGroup.GetScriptingDefineSymbolsForGroup();

            foreach (var (optionAttribute, enumType) in ReflectionProvider.GetAttributeEnumSets<BuildOptionEnumAttribute>()) {
                if (optionAttribute.buildTargetGroup == BuildTargetGroup.Unknown || optionAttribute.buildTargetGroup == targetAttribute.buildTargetGroup) {
                    foreach (var ob in Enum.GetValues(enumType)) {
                        optionDic[ob.ToString()] = false;
                    }
                }
            }
        } else {
            defineSymbols = string.Empty;
            optionDic.Clear();
        }

        applicationIdentifier = PlayerSettings.applicationIdentifier;
        bundleVersion = PlayerSettings.bundleVersion;

        foreach (var logType in EnumUtil.AsSpan<LogType>()) {
            stackTraceDic[logType] = PlayerSettings.GetStackTraceLogType(logType);
        }

        developmentBuild = EditorUserBuildSettings.development;
        autoConnectProfile = EditorUserBuildSettings.connectProfiler;
        deepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;
        scriptDebugging = EditorUserBuildSettings.allowDebugging;
    }
}

public struct BuildRecord {

    public BuildResult result;
    public BuildTarget buildTarget;
    public string outputPath;
    public DateTime startTime;
    public DateTime endTime;
    public TimeSpan buildTime;
    public string memo;
}