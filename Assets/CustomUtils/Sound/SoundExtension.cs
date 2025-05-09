﻿using CustomUtils.Sound.NewSoundSystem;
using UnityEngine;
using UnityEngine.Audio;

public static class SoundExtension {

    public static bool TryFindMatchingGroups(this AudioMixer audioMixer, string subPath, out AudioMixerGroup[] mixerGroups) => (mixerGroups = audioMixer.FindMatchingGroups(subPath))?.Length > 0;

    public static bool IsValid(this AudioSource audioSource) {
        if (audioSource == null || audioSource.clip == null) {
            return false;
        }

        return audioSource.clip.loadState == AudioDataLoadState.Loaded;
    }

    // TODO. SoundTrackEvent 제거 혹은 수정
    public static void Set(this AudioSource audioSource, SoundTrackEvent trackEvent) {
        if (trackEvent != null) {
            audioSource.clip = trackEvent.clip;
            audioSource.loop = trackEvent.loop;
        }
    }

    // TODO. SoundTrackEvent 제거 혹은 수정
    public static void PlayOneShot(this AudioSource audioSource, SoundTrackEvent trackEvent) {
        if (trackEvent != null) {
            audioSource.PlayOneShot(trackEvent.clip);
        }
    }

    // TODO. SoundTrackEvent 제거 혹은 수정
    public static void Play(this AudioSource audioSource, SoundTrackEvent trackEvent) {
        if (trackEvent != null) {
            audioSource.clip = trackEvent.clip;
            audioSource.loop = trackEvent.loop;
            audioSource.Play();
        }
    }

    public static string GetDescription(this SOUND_TRACK_ERROR type) {
        switch (type) {
            case SOUND_TRACK_ERROR.NULL_EVENTS:
                return $"{nameof(SoundTrack.eventList)} is null. Fatal issue";
            case SOUND_TRACK_ERROR.EMPTY_EVENTS:
                return $"{nameof(SoundTrack.eventList)} is empty.";
            case SOUND_TRACK_ERROR.EXCEPTION_EVENT:
                return $"{nameof(SoundTrack.eventList)} is exception catch";
            case SOUND_TRACK_ERROR.CLIP_LOAD_FAILED:
                return $"{nameof(SoundTrack.eventList)} is invalid. Some ${nameof(AudioClip)} was invalid {nameof(AudioClip.loadState)}.";
            default:
                return string.Empty;
        }
    }
}