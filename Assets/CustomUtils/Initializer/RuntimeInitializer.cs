using UnityEngine;

public class RuntimeInitializer {

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void OnInitializeOnLoadBeforeSplashScreen() { }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void OnInitializeOnLoadAfterAssembliesLoaded() { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnInitializeOnLoadSubsystemRegistration() {
        Service.StartService(DEFAULT_SERVICE_TYPE.INIT_MAIN_THREAD);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnInitializeOnLoadBeforeSceneLoad() { } 
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnInitializeOnLoadAfterSceneLoad() { } 
}
