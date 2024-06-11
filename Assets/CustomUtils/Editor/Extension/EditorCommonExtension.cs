using UnityEditor;

public static class EditorCommonExtension {
    
    public static BuildTargetGroup GetTargetGroup(this BuildTarget type) => BuildPipeline.GetBuildTargetGroup(type);
}
