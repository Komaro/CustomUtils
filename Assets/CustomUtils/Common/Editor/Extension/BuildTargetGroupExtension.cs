using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build;
#endif

public static class BuildTargetGroupExtension {

    public static string GetScriptingDefineSymbolsForGroup(this BuildTargetGroup targetGroup) {
#if UNITY_6000_0_OR_NEWER
        return PlayerSettings.GetScriptingDefineSymbols(targetGroup.GetNamedBuildTarget());
#else
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
    }

#if UNITY_6000_0_OR_NEWER
    public static NamedBuildTarget GetNamedBuildTarget(this BuildTargetGroup targetGroup) => NamedBuildTarget.FromBuildTargetGroup(targetGroup);
#endif
}

