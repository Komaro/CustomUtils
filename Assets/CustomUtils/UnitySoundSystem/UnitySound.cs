using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using CustomUtils.Sound.NewSoundSystem;

public abstract class UnitySound : SoundBase<UnitySoundCore> {

    protected ImmutableDictionary<SoundType, AudioMixerGroup> audioMixerGroupDic = ImmutableDictionary<SoundType, AudioMixerGroup>.Empty;
    
    protected readonly Dictionary<SoundType, AudioSource> audioSourceDic = new();
    protected readonly Dictionary<SoundType, Queue<AudioSource>> audioSourceQueueDic = new();
    protected readonly Dictionary<SoundType, Queue<AudioSource>> audioSourcePlayingDic = new();

    protected readonly Dictionary<SoundType, int> maxQueueDic = new();

    protected readonly ConcurrentDictionary<SoundType, GameObject> audioSourceRootDic = new();

    protected readonly object queueLock = new();

    protected UnitySound(UnitySoundCore soundCore) : base(soundCore) { }

    public override void Init() {
        if (soundCore.TryGetAudioMixerGroupDic(masterType, out var groupDic)) {
            audioMixerGroupDic = groupDic.ToImmutableDictionary();
            foreach (var (controlType, mixerGroup) in audioMixerGroupDic) {
                audioSourceQueueDic.AutoAdd(controlType);
                audioSourcePlayingDic.AutoAdd(controlType);
                maxQueueDic.AutoAdd(controlType, 0);
            }
        }

        if (soundCore.TryGetAssetInfoList(representControlType, out var infoList)) {
            foreach (var info in infoList) {
                UpdateSoundAssetInfo(info);
            }
        }
    }
    
    protected abstract override void UpdateSoundAssetInfo(SoundAssetInfo info);
    
    protected override void UpdateQueue() {
        lock (queueLock) {
            foreach (var (soundType, soundQueue) in audioSourcePlayingDic) {
                var count = soundQueue.Count;
                while (count-- > 0) {
                    if (soundQueue.TryDequeue(out var audioSource) == false) {
                        continue;
                    }

                    if (audioSource.isPlaying) {
                        soundQueue.Enqueue(audioSource);
                    } else {
                        audioSourceQueueDic.AutoAdd(soundType, audioSource);
                    }
                }
            }
        }
    }
    
    public abstract override void PlayRefresh();
    
    public virtual void PlayOneShot(SoundTrackEvent trackEvent) {
        if (TryGetMasterAudioSource(out var audioSource)) {
            audioSource.PlayOneShot(trackEvent.clip);
        }
    }
    
    public abstract override void SubmitSoundOrder(NewSoundOrder order);
    
    protected bool TryGetMasterAudioSource(out AudioSource audioSource) => (audioSource = GetMasterAudioSource()) != null;
    protected virtual AudioSource GetMasterAudioSource() => GetAudioSource(masterType);
    
    public bool TryGetAudioMixerGroup(Enum enumValue, out AudioMixerGroup mixerGroup) => (mixerGroup = GetAudioMixerGroup(enumValue)) != null;
    public AudioMixerGroup GetAudioMixerGroup(Enum enumValue) => TryGetSoundType(enumValue, out var soundType) ? GetAudioMixerGroup(soundType) : null; 

    public bool TryGetAudioMixerGroup(SoundType soundType, out AudioMixerGroup mixerGroup) => (mixerGroup = GetAudioMixerGroup(soundType)) != null;
    public virtual AudioMixerGroup GetAudioMixerGroup(SoundType soundType) => audioMixerGroupDic.TryGetValue(soundType, out var mixerGroup) ? mixerGroup : null;
    
    protected bool TryGetRepresentAudioSource(out AudioSource audioMixer) => (audioMixer = GetRepresentAudioSource()) != null;
    protected virtual AudioSource GetRepresentAudioSource() => GetAudioSource(representControlType);
    
    protected bool TryGetAudioSource(Enum enumValue, out AudioSource audioSource) => (audioSource = GetAudioSource(enumValue)) != null; 
    protected AudioSource GetAudioSource(Enum enumValue) => TryGetSoundType(enumValue, out var soundType) ? GetAudioSource(soundType) : null;

    protected bool TryGetAudioSource(SoundType soundType, out AudioSource audioSource) => (audioSource = GetAudioSource(soundType)) != null;
    
    [TestRequired]
    protected virtual AudioSource GetAudioSource(SoundType soundType) {
        lock (queueLock) {
            if (audioSourceDic.TryGetValue(soundType, out var audioSource)) {
                return audioSource;
            }

            if (TryCreateAudioSource(soundType, out audioSource)) {
                audioSourceDic.AutoAdd(soundType, audioSource);
                return audioSource;
            }
        }

        Logger.TraceError($"{soundType} is an invalid {nameof(SoundType)}");
        return null;
    }
    
    protected bool TryGetQueueAudioSource(Enum controlType, int maxCreateCount, out AudioSource audioSource) => (audioSource = GetQueueAudioSource(controlType, maxCreateCount)) != null;

    protected AudioSource GetQueueAudioSource(Enum enumValue, int maxCreateCount) => TryGetSoundType(enumValue, out var soundType) ? GetQueueAudioSource(soundType, maxCreateCount) : null;

    protected virtual AudioSource GetQueueAudioSource(SoundType soundType, int maxCreateCount) {
        if (audioMixerGroupDic.ContainsKey(soundType) == false) {
            Logger.TraceError($"{soundType} is missing {nameof(AudioMixerGroup)}");
            return null;
        }

        lock (queueLock) {
            if (audioSourceQueueDic.TryGetValue(soundType, out var idleQueue) && audioSourcePlayingDic.TryGetValue(soundType, out var playingQueue)) {
                if (idleQueue.TryDequeue(out var audioSource)) {
                    playingQueue.Enqueue(audioSource);
                    return audioSource;
                }

                if (maxQueueDic.TryGetValue(soundType, out var count) && count < maxCreateCount && TryCreateAudioSource(soundType, out audioSource)) {
                    maxQueueDic.AutoIncreaseAdd(soundType);
                    playingQueue.Enqueue(audioSource);
                    return audioSource;
                }
            }
        }
        
        Logger.TraceLog($"Cannot create more than maxCreateCount {nameof(AudioSource)} for the given {soundType}", Color.red);
        return null;
    }

    protected bool TryCreateAudioSource(Enum enumValue, out AudioSource audioSource) => (audioSource = CreateAudioSource(enumValue)) != null;
    protected AudioSource CreateAudioSource(Enum enumValue) => soundCore.TryGetSoundType(enumValue, out var soundType) ? CreateAudioSource(soundType) : null;
    
    protected bool TryCreateAudioSource(SoundType soundType, out AudioSource audioSource) => (audioSource = CreateAudioSource(soundType)) != null;

    [TestRequired]
    protected virtual AudioSource CreateAudioSource(SoundType soundType) {
        if (audioSourceRootDic.TryGetValue(soundType, out var go) && TryCreateAudioSource(go, soundType, out var audioSource)) {
            return audioSource;
        }

        if (soundCore.TryGetSoundObject(soundType, out go) && TryCreateAudioSource(go, soundType, out audioSource)) {
            audioSourceRootDic.AutoAdd(soundType, go);
            return audioSource;
        }
        
        Logger.TraceError($"Missing ({soundType}) {nameof(AudioMixerGroup)}");
        return null;
    }

    protected bool TryCreateAudioSource(GameObject go, SoundType soundType, out AudioSource audioSource) => (audioSource = CreateAudioSource(go, soundType)) != null;
    
    [TestRequired]
    protected virtual AudioSource CreateAudioSource(GameObject go, SoundType soundType) {
        if (audioMixerGroupDic.TryGetValue(soundType, out var mixerGroup)) {
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = mixerGroup;
            audioSource.playOnAwake = false;
            audioSource.mute = isMute;
            return audioSource;
        }

        return null;
    }

    protected virtual void Unload(AudioSource audioSource) {
        if (audioSource.IsValid()) {
            audioSource.clip.UnloadAudioData();
            audioSource.clip = null;
        }
    }
    
    [TestRequired]
    public override void Unload(SoundType soundType) {
        lock (queueLock) {
            foreach (var audioSource in GetAllAudioSource(soundType)) {
                Unload(audioSource);
            }
        }
    }

    [TestRequired]
    public override void UnloadAll() {
        lock (queueLock) {
            foreach (var audioSource in GetAllAudioSource()) {
                Unload(audioSource);
            }
        }
    }

    public override void SetMute(bool isMute) {
        this.isMute = isMute;
        foreach (var audioSource in GetAllAudioSource()) {
            audioSource.mute = isMute;
        }
    }

    [TestRequired]
    private IEnumerable<AudioSource> GetAllAudioSource() {
        foreach (var audioSource in audioSourceDic.Values.Concat(audioSourceQueueDic.Values.SelectMany()).Concat(audioSourcePlayingDic.Values.SelectMany())) {
            if (audioSource != null) {
                yield return audioSource;
            }
        }
    }

    [TestRequired]
    private IEnumerable<AudioSource> GetAllAudioSource(SoundType soundType) {
        foreach (var audioSource in audioSourceDic.Search(soundType).Concat(audioSourceQueueDic.Search(soundType).SelectMany()).Concat(audioSourcePlayingDic.Search(soundType).SelectMany())) {
            if (audioSource != null) {
                yield return audioSource;
            }
        }
    }
}