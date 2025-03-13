#if UNITY_EDITOR

using UnityEditor;

public static partial class Service {
    
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorApplication.isPlayingOrWillChangePlaymode == false) {
            Initialize();
        }
    }
}

#endif
