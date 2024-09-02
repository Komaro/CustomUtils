using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class UnityBuildInteractionInterface : IPostprocessBuildWithReport {

    private static BuilderEx _builder;

    public int callbackOrder => 1000;

    public static bool TryAttachBuilder(Type type, out BuilderEx builder) => (builder = AttachBuilder(type)) != null;

    public static BuilderEx AttachBuilder(Type type) {
        if (BuilderEx.TryCreateBuilder(type, out _builder)) {
            return _builder;
        }

        Debug.LogError($"Failed to create '{type.Name}'");
        return null;
    }

    public void OnPostprocessBuild(BuildReport report) => _builder?.PostProcess(report.summary);
    
    /// <summary>
    /// Using Only Command Line Build
    /// </summary>
    public static void BuildOnCLI() {
        BuildConfigProvider.LoadOnCLI();
        if (BuildConfigProvider.TryGetValue<string>("buildType", out var enumText)) {
            foreach (var (enumAttribute, type) in ReflectionProvider.GetAttributeEnumInfos<BuildTypeEnumAttribute>()) {
                if (Enum.TryParse(type, enumText, out var ob) && ob is Enum enumValue && BuilderEx.TryCreateBuilder(enumValue, out _builder)) {
                    if (BuildConfigProvider.TryGetValue<string>("configPath", out var path)) {
                        BuildConfigProvider.Load(path);
                    } else {
                        foreach (var configType in ReflectionProvider.GetSubClassTypes<BuildConfig>()) {
                            if (configType.TryGetCustomAttribute<BuildConfigAttribute>(out var attribute) && attribute.buildType.Equals(enumValue)) {
                                BuildConfigProvider.Load($"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorBuildServiceEx)}/{configType.Name}{Constants.Extension.JSON}");
                                break;
                            }
                        }
                    }
        
                    _builder.StartBuild();
                    break;
                }
            }
        }

        if (_builder == null) {
            Debug.LogError($"Fail to create {nameof(BuilderEx)}");
        }
    }
}