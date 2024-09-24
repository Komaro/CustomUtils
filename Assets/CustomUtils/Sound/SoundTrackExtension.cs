using UnityEngine;

public static class SoundTrackExtension {

    public static void Set(this AudioSource audioSource, SoundTrackEvent trackEvent) {
        if (trackEvent != null) {
            audioSource.clip = trackEvent.clip;
            audioSource.loop = trackEvent.loop;
        }
    }

    public static void Play(this AudioSource audioSource, SoundTrackEvent trackEvent) {
        if (trackEvent != null) {
            audioSource.clip = trackEvent.clip;
            audioSource.loop = trackEvent.loop;
            audioSource.Play();
        }
    }
}