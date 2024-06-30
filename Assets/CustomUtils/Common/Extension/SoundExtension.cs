using UnityEngine;

public static class SoundExtension {
    
    public static bool IsValidClip(this AudioSource audioSource) => audioSource.clip != null && audioSource.clip.loadState == AudioDataLoadState.Loaded;

}
