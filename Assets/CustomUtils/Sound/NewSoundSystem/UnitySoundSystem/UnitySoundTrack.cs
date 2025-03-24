using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitySoundTrack", menuName = "Sound/Create UnitySoundTrack")]
public class UnitySoundTrack : NewSoundTrack<UnitySoundTrackEvent> {
    
#if UNITY_EDITOR
    
    private static readonly List<IDisposable> _disposableList = new();
    
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
                foreach (var trackEvent in events) {
                    Logger.TraceLog($"Play || {name} || {trackEvent.clip.name}", Color.cyan);
                    audioSource.PlayOneShot(trackEvent);
                }
                break;
            case TRACK_TYPE.RANDOM:
                if (events.TryGetRandom(out var randomEvent)) {
                    Logger.TraceLog($"Play || {name} || {randomEvent.clip.name}", Color.cyan);
                    audioSource.Play(randomEvent);
                }
                break;
            default:
                var startTime = 0f;
                foreach (var trackEvent in events) {
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
}