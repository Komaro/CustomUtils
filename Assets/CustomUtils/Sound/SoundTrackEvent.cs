using System;
using UnityEngine;

[Serializable]
public class SoundTrackEvent {
    
    public AudioClip clip;
    public bool loop = false;
    public float startTime = 0;
    
    public void Unload() {
        if (clip != null && clip.loadState == AudioDataLoadState.Loaded) {
            clip.UnloadAudioData();
        }
    }

    public bool IsValidClip() => clip != null;
}
