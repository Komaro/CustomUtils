using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundTrackExtension", menuName = "Sound/Create Extension Track")]
public class SoundTrackEx : ScriptableObject {
    
    public GlobalEnum<SoundTrackEnumAttribute> trackType = new();
    public SoundTrackEvent[] eventList;

    private static List<IDisposable> _disposableList = new();
    
    public virtual void PreviewPlay(AudioSource audioSource) {
        if (audioSource == null) {
            Logger.TraceError($"{nameof(audioSource)} is Null");
            return;
        }
        
        PlayPreviewSoundEvent(audioSource);
    }

    // 런타임에 아래 코드 사용 시 싱크 문제가 발생할 수 있음.
    protected virtual void PlayPreviewSoundEvent(AudioSource audioSource) {
        if (Application.isEditor == false || Application.isPlaying) {
            Logger.TraceError("Do not use this method outside of the editor or during runtime.");
            return;
        }
        
        if (audioSource.isPlaying) {
            PreviewStop();
        }
        
        switch (trackType.Value) {
            case TRACK_TYPE.OVERLAP:
                eventList.ForEach(x => {
                    Logger.TraceLog($"Play || {name} || {x.clip.name}", Color.cyan);
                    audioSource.PlayOneShot(x.clip);
                });
                break;
            case TRACK_TYPE.RANDOM:
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

    protected virtual void StopPreviewSoundEvent() => _disposableList.SafeClear(x => x.Dispose());

    public virtual void PreviewStop() {
        Logger.TraceLog($"Stop || {name}", Color.red);
        _disposableList.ForEach(x => x.Dispose());
        _disposableList.Clear();
    }
    
    public virtual void UnloadAudioClip() => eventList?.ForEach(x => x.Unload());

    public virtual bool IsValid(out SOUND_TRACK_ERROR error) {
        try {
            if (eventList == null) {
                error = SOUND_TRACK_ERROR.EVENT_LIST_NULL;
                return false;
            }
            
            if (eventList.Length <= 0) {
                error = SOUND_TRACK_ERROR.EVENT_LIST_EMPTY;
                return false;
            }
            
            foreach (var track in eventList) {
                if (track == null) {
                    error = SOUND_TRACK_ERROR.EVENT_NULL;
                    return false;
                }

                if (track.IsValidClip() == false) {
                    error = SOUND_TRACK_ERROR.CLIP_INVALID;
                    return false;
                }

                if (track.clip.loadState == AudioDataLoadState.Failed) {
                    error = SOUND_TRACK_ERROR.CLIP_LOAD_FAILED;
                    return false;
                }
            }
        } catch {
            error = SOUND_TRACK_ERROR.EVENT_EXCEPTION;
            return false;
        }
        
        error = default;
        return true;
    }
    
    public virtual bool IsValid() => eventList is { Length: > 0 } && eventList.Any(x => x != null && x.IsValidClip() && x.clip.loadState != AudioDataLoadState.Failed);
}

public class SoundTrackEnumAttribute : PriorityAttribute { }

public enum TEST_SOUND_TRACK_TYPE {
    TEST_PLAY
}

[SoundTrackEnum(priority = 25)]
public enum TEST_GLOBAL_ENUM_01 {
    TEST_01,
    TEST_02,
    TEST_03,
}

[SoundTrackEnum(priority = 10)]
public enum TEST_GLOBAL_ENUM_02 {
    TEST_01,
    TEST_02,
    TEST_03,
}