
using UnityEditor;

[EditorBuildDrawer(SAMPLE_BUILD_TYPE.STANDALONE)]
public class SampleStandaloneBuildDrawer : EditorBuildDrawer<SampleStandaloneBuildConfig, SampleStandaloneBuildConfig.NullBuildConfig> {


    public SampleStandaloneBuildDrawer(EditorWindow window) : base(window) { }
}

[BuildConfig(SAMPLE_BUILD_TYPE.STANDALONE, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone)]
public class SampleStandaloneBuildConfig : BuildConfig {

    public override bool IsNull() => this is NullBuildConfig;
    
    [BuildConfig]
    public class NullBuildConfig : SampleStandaloneBuildConfig { }
}
