using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundTrack", menuName = "Sound/Create Track")]
public class SoundTrack : ScriptableObject {

    public E_TRACK_TYPE type;
    public int limitPlayCount = 0;
    
    public SoundTrackEvent[] eventList;
    
    private static List<IDisposable> _disposableList = new(); 
    
    public void PreviewPlay(AudioSource audioSource) {
        if (audioSource == null) {
            Logger.TraceError($"{nameof(audioSource)} is Null");
            return;
        }
        
        PlayPreviewSoundEvent(audioSource);
    }

    // 런타임에 아래 코드 사용 시 싱크 문제가 발생할 수 있음.
    protected void PlayPreviewSoundEvent(AudioSource audioSource) {
        if (audioSource.isPlaying) {
            PreviewStop();
        }
        
        switch (type) {
            case E_TRACK_TYPE.OVERLAP:
                eventList.ForEach(x => {
                    Logger.TraceLog($"Play || {name} || {x.clip.name}", Color.cyan);
                    audioSource.PlayOneShot(x.clip);
                });
                break;
            case E_TRACK_TYPE.RANDOM:
                if (eventList.TryGetRandom(out var randomEvent)) {
                    Logger.TraceLog($"Play || {name} || {randomEvent.clip.name}", Color.cyan);
                    audioSource.PlayOneShot(randomEvent.clip);
                }
                break;
            default:
                var startTime = 0f;
                foreach (var soundEvent in eventList) {
                    _disposableList.Add(Observable.EveryUpdate().Delay(TimeSpan.FromSeconds(startTime - 0.1f)).First().Subscribe(_ => {
                        Logger.TraceLog($"Play || {name} || {soundEvent.clip.name}", Color.cyan);
                        audioSource.clip = soundEvent.clip;
                        audioSource.loop = soundEvent.loop;
                        audioSource.Play();
                    }));
                    startTime += soundEvent.clip.length;
                }
                break;
        }
    }

    protected void StopPreviewSoundEvent() => _disposableList.SafeClear(x => x.Dispose());

    public void PreviewStop() {
        Logger.TraceLog($"Stop || {name}", Color.red);
        _disposableList.ForEach(x => x.Dispose());
        _disposableList.Clear();
    }
    
    public void UnloadAudioClip() => eventList?.ForEach(x => x.Unload());

    public bool IsValid(out E_SOUND_TRACK_ERROR error) {
        try {
            if (eventList == null) {
                error = E_SOUND_TRACK_ERROR.EVENT_LIST_NULL;
                return false;
            }
            
            if (eventList.Length <= 0) {
                error = E_SOUND_TRACK_ERROR.EVENT_LIST_EMPTY;
                return false;
            }
            
            foreach (var track in eventList) {
                if (track == null) {
                    error = E_SOUND_TRACK_ERROR.EVENT_NULL;
                    return false;
                }

                if (track.IsValidClip() == false) {
                    error = E_SOUND_TRACK_ERROR.CLIP_INVALID;
                    return false;
                }

                if (track.clip.loadState == AudioDataLoadState.Failed) {
                    error = E_SOUND_TRACK_ERROR.CLIP_LOAD_FAILED;
                    return false;
                }
            }
        } catch {
            error = E_SOUND_TRACK_ERROR.EVENT_EXCEPTION;
            return false;
        }
        
        error = default;
        return true;
    }
    
    public bool IsValid() => eventList is { Length: > 0 } && eventList.Any(x => x != null && x.IsValidClip() && x.clip.loadState != AudioDataLoadState.Failed);
}

public enum E_TRACK_TYPE {
    DEFAULT,
    OVERLAP,
    RANDOM,
    LIMIT_PLAY,
}

public enum E_SOUND_TRACK_ERROR {
    NONE,
    EVENT_NULL,
    EVENT_EXCEPTION,
    
    EVENT_LIST_NULL,
    EVENT_LIST_EMPTY,
    
    CLIP_INVALID,
    CLIP_LOAD_FAILED,
}

public static class SoundTrackErrorExtension {
    public static string GetDescription(this E_SOUND_TRACK_ERROR type) {
        switch (type) {
            case E_SOUND_TRACK_ERROR.EVENT_LIST_NULL:
                return $"{nameof(SoundTrack.eventList)} is Null. Fatal Issue";
            case E_SOUND_TRACK_ERROR.EVENT_LIST_EMPTY:
                return $"{nameof(SoundTrack.eventList)} is Empty.";
            case E_SOUND_TRACK_ERROR.EVENT_EXCEPTION:
                return $"{nameof(SoundTrack.eventList)} is Exception Catch";
            case E_SOUND_TRACK_ERROR.CLIP_LOAD_FAILED:
                return $"{nameof(SoundTrack.eventList)} is Invalid. Some ${nameof(AudioClip)} was Invalid {nameof(AudioClip.loadState)}.";
            default:
                return string.Empty;
        }
    }
}
