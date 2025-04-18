﻿using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundTrack", menuName = "Sound/Create SoundTrack")]
public class SoundTrack : ScriptableObject {
    
    public GlobalEnum<SoundTrackEnumAttribute> trackType = new();
    public SoundTrackEvent[] eventList;

#if UNITY_EDITOR
    
    private static List<IDisposable> _disposableList = new();
    
    public virtual void PreviewPlay(AudioSource audioSource) {
        if (audioSource == null) {
            Logger.TraceError($"{nameof(audioSource)} is null");
            return;
        }
        
        PlayPreviewSoundEvent(audioSource);
    }

    protected virtual void PlayPreviewSoundEvent(AudioSource audioSource) {
        if (Application.isEditor == false || Application.isPlaying) {
            Logger.TraceError("Do not use this method outside of the editor or during runtime.");
            return;
        }
        
        if (audioSource.isPlaying) {
            PreviewStop();
        }

        audioSource.time = 0;
        switch (trackType.Value) {
            case TRACK_TYPE.OVERLAP:
                foreach (var trackEvent in eventList) {
                    Logger.TraceLog($"Play || {name} || {trackEvent.clip.name}", Color.cyan);
                    audioSource.PlayOneShot(trackEvent);
                }
                break;
            case TRACK_TYPE.RANDOM:
                if (eventList.TryGetRandom(out var randomEvent)) {
                    Logger.TraceLog($"Play || {name} || {randomEvent.clip.name}", Color.cyan);
                    audioSource.Play(randomEvent);
                }
                break;
            default:
                var startTime = 0f;
                foreach (var trackEvent in eventList) {
                    _disposableList.Add(Observable.EveryUpdate().Delay(TimeSpan.FromSeconds(startTime - 0.1f)).First().Subscribe(_ => {
                        Logger.TraceLog($"Play || {name} || {trackEvent.clip.name}", Color.cyan);
                        audioSource.Play(trackEvent);
                    }));
                    startTime += trackEvent.clip.length;
                }
                break;
        }
    }

    protected virtual void StopPreviewSoundEvent() => _disposableList.SafeClear(x => x.Dispose());

    public virtual void PreviewStop() {
        Logger.TraceLog($"Stop || {name}", Color.red);
        _disposableList.ForEach(x => x.Dispose());
        _disposableList.Clear();
    }
    
#endif
    
    public virtual void UnloadAudioClip() => eventList?.ForEach(x => x.Unload());

    protected virtual bool IsValidEventList(out SOUND_TRACK_ERROR error) {
        if (eventList == null) {
            error = SOUND_TRACK_ERROR.NULL_EVENTS;
            return false;
        } 
        
        if (eventList.Length <= 0) {
            error = SOUND_TRACK_ERROR.EMPTY_EVENTS;
            return false;
        }

        error = default;
        return true;
    }

    protected virtual bool IsValidSoundTrackEvent(SoundTrackEvent eventTrack, out SOUND_TRACK_ERROR error) {
        if (eventTrack == null) {
            error = SOUND_TRACK_ERROR.NULL_EVENT;
            return false;
        }

        if (eventTrack.IsValidClip() == false) {
            error = SOUND_TRACK_ERROR.INVALID_CLIP;
            return false;
        }

        if (eventTrack.clip.loadState == AudioDataLoadState.Failed) {
            error = SOUND_TRACK_ERROR.CLIP_LOAD_FAILED;
            return false;
        }

        error = default;
        return true;
    }
    
    public virtual bool IsValid(out SOUND_TRACK_ERROR error) {
        try {
            if (IsValidEventList(out error) == false) {
                return false;
            }
            
            foreach (var track in eventList) {
                if (IsValidSoundTrackEvent(track, out error) == false) {
                    return false;
                }
            }
        } catch {
            error = SOUND_TRACK_ERROR.EXCEPTION_EVENT;
            return false;
        }
        
        error = default;
        return true;
    }
    
    public virtual bool IsValid() => IsValidEventList(out _) && eventList.All(x => IsValidSoundTrackEvent(x, out _));
}

public class SoundTrackEnumAttribute : PriorityAttribute { }

[SoundTrackEnum]
public enum TRACK_TYPE {
    DEFAULT,
    OVERLAP,
    RANDOM,
    LIMIT_PLAY,
}

public enum SOUND_TRACK_ERROR {
    NONE,
    
    NULL_EVENT,
    EXCEPTION_EVENT,
    
    NULL_EVENTS,
    EMPTY_EVENTS,
    
    INVALID_CLIP,
    CLIP_LOAD_FAILED,
}