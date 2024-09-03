using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

[Builder(SAMPLE_BUILD_TYPE.STANDALONE, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone)]
[Alias("Sample Standalone Build")]
public class Sample_Builder_Standalone : Builder {

    protected override void OnPreProcess(ref BuildPlayerOptions options) {
        if (options.locationPathName.IsExtension() == false) {
            options.locationPathName = Path.Combine(options.locationPathName, buildType.ToString().GetForceTitleCase(), buildType.ToString().GetForceTitleCase().AutoSwitchExtension(Constants.Extension.EXE));
        }
        
        SystemUtil.EnsureDirectoryExists(options.locationPathName);
        
        Logger.TraceLog(nameof(OnPreProcess));
    }

    protected override void OnPostProcess(BuildSummary summary) {
        Logger.TraceLog(nameof(OnPostProcess));
    }
}
