#if UNITY_EDITOR

using UnityEditor;

public static partial class Service {

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        if (EditorApplication.isPlayingOrWillChangePlaymode == false) {
            Initialize();
        }
        
        StartService(DEFAULT_SERVICE_TYPE.START_MAIN_THREAD);
    }
}

#endif
