using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

[Builder(SAMPLE_BUILD_TYPE.STANDALONE, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone)]
public class Sample_Builder_Standalone : BuilderEx {

    protected override void OnPreProcess(ref BuildPlayerOptions options) {
        if (options.locationPathName.IsExtension() == false) {
            options.locationPathName = Path.Combine(options.locationPathName, buildType.ToString().GetForceTitleCase().AutoSwitchExtension(Constants.Extension.EXE));
        }
        
        Logger.TraceLog(nameof(OnPreProcess));
    }

    protected override void OnPostProcess(BuildSummary summary) {
        Logger.TraceLog(nameof(OnPostProcess));
    }
}
