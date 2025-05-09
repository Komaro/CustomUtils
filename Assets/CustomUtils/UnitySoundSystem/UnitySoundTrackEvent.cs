﻿using System;
using CustomUtils.Sound.NewSoundSystem;
using UnityEngine;

[Serializable]
[TestRequired]
public class UnitySoundTrackEvent : ISoundTrackEvent {

    public AudioClip clip;
    public bool loop;
    public float startTime;
    
    public UnitySoundTrackEvent() { }
    public UnitySoundTrackEvent(AudioClip clip) => this.clip = clip;

    public void Unload() {
        if (clip != null && clip.loadState == AudioDataLoadState.Loaded) {
            clip.UnloadAudioData();
        }
    }

    public bool IsValid() => clip != null;
}