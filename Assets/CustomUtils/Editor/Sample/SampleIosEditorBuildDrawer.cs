using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class SampleIosEditorBuildDrawer : EditorBuildDrawer<SampleIosBuildConfig, SampleIosBuildConfig.NullBuildConfig> {

    protected override string CONFIG_NAME => $"{nameof(SampleIosBuildConfig)}{Constants.Extension.JSON}";
    
    public SampleIosEditorBuildDrawer(EditorWindow window) : base(window) { }
}

[EditorBuildDrawer(typeof(Sample_Builder_Ios))]
public class SampleIosEditorBuildSettingDrawer : SampleIosEditorBuildDrawer {

    public SampleIosEditorBuildSettingDrawer(EditorWindow window) : base(window) { }
    
    public override void Draw() {
        base.Draw();
        
        EditorCommon.DrawSeparator();
        
        EditorGUILayout.LabelField("iOS 옵션", Constants.Draw.AREA_TITLE_STYLE);
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            EditorCommon.DrawLabelTextField(nameof(config.buildNumber), ref config.buildNumber, 200f);
            EditorCommon.DrawLabelTextField(nameof(config.appleDeveloperTeamID), ref config.appleDeveloperTeamID, 200f);
            EditorCommon.DrawLabelTextField(nameof(config.iOSManualProvisioningProfileID), ref config.iOSManualProvisioningProfileID, 200f);
            EditorCommon.DrawEnumPopup(nameof(config.iOSManualProvisioningProfileType), ref config.iOSManualProvisioningProfileType, 250f);
        }
    }
}

[BuildConfig(BuildTarget.iOS, BuildTargetGroup.iOS)]
public class SampleIosBuildConfig : BuildConfig {
    
    public string buildNumber;
    public string appleDeveloperTeamID;
    public string iOSManualProvisioningProfileID;
    public ProvisioningProfileType iOSManualProvisioningProfileType;

    public SampleIosBuildConfig() {
        buildNumber = PlayerSettings.iOS.buildNumber;
        appleDeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID;
        iOSManualProvisioningProfileID = PlayerSettings.iOS.iOSManualProvisioningProfileID;
        iOSManualProvisioningProfileType = PlayerSettings.iOS.iOSManualProvisioningProfileType;
    }
    
    public override bool IsNull() => this is NullBuildConfig;
    
    [BuildConfig(BuildTarget.NoTarget, BuildTargetGroup.Unknown)]
    public class NullBuildConfig : SampleIosBuildConfig { }
}

[DefineSymbolEnum]
public enum SAMPLE_DEFINE_TYPE {
    TEST,
    
    [EnumValue("========")]
    DEBUG,
    SAMPLE,
}