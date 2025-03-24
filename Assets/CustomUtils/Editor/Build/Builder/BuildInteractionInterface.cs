using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildInteractionInterface : IPostprocessBuildWithReport {

    private static Builder _builder;

    public int callbackOrder => 1000;

    public static bool TryAttachBuilder(Type type, out Builder builder) => (builder = AttachBuilder(type)) != null;

    public static Builder AttachBuilder(Type type) {
        if (Builder.TryCreateBuilder(type, out _builder)) {
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
        Debug.Log($"Start {nameof(BuildOnCLI)}");
        BuildConfigProvider.LoadOnCLI();
        if (BuildConfigProvider.TryGetValue<string>("buildType", out var enumText)) {
            foreach (var type in ReflectionProvider.GetAttributeEnumTypes<BuildTypeEnumAttribute>()) {
                if (Enum.TryParse(type, enumText, out var ob) && ob is Enum enumValue && Builder.TryCreateBuilder(enumValue, out _builder)) {
                    if (BuildConfigProvider.TryGetValue<string>("configPath", out var path)) {
                        BuildConfigProvider.Load(path);
                    } else {
                        foreach (var configType in ReflectionProvider.GetSubTypesOfType<BuildConfig>()) {
                            if (configType.TryGetCustomAttribute<BuildConfigAttribute>(out var attribute) && attribute.buildType != null && attribute.buildType.Equals(enumValue)) {
                                BuildConfigProvider.Load($"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorBuildService)}/{configType.Name}{Constants.Extension.JSON}");
                                break;
                            }
                        }
                    }

                    _builder.StartBuild();
                    break;
                }
            }
        } else {
            throw new BuildFailedException("buildType parameter missing");
        }

        if (_builder == null) {
            throw new BuildFailedException($"Fail to create {nameof(Builder)}");
        }
    }
}