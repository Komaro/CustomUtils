using System;
using UnityEngine;

[Serializable]
public class SoundTrackEvent {
    
    public AudioClip clip;
    public bool loop = false;
    public float startTime = 0;

#if UNITY_EDITOR
    private float _cursor;
#endif
    
    public void Unload() {
        if (clip != null && clip.loadState == AudioDataLoadState.Loaded) {
            clip.UnloadAudioData();
        }
    }

    public bool IsValidClip() => clip != null;
}
