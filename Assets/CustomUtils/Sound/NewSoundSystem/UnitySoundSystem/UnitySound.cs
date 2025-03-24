using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public abstract class UnitySound : NewSoundBase {

    protected ConcurrentDictionary<Enum, AudioMixerGroup> audioMixerGroupDic = new();
    protected ConcurrentDictionary<Enum, AudioSource> audioSourceDic = new();

    protected ConcurrentDictionary<Enum, Queue<AudioSource>> audioSourceQueueDic = new();
    protected ConcurrentDictionary<Enum, Queue<AudioSource>> audioSourcePlayingDic = new();
    protected ConcurrentDictionary<Enum, int> maxQueueDic = new();

    protected object updateLock = new();

    protected Dictionary<Enum, GameObject> audioSourceRootDic = new();

    protected UnitySound(SoundCoreBase soundCore) : base(soundCore) { }

    public override void Init() {
        if (masterEnum == null) {
            Logger.TraceError($"{nameof(Init)} Failed. {nameof(masterEnum)} is Null. Check {nameof(MasterSoundAttribute)}");
            return;
        }

        if (representControlEnum == null && soundControlSet.Count > 1) {
            representControlEnum = soundControlSet.FirstOrDefault(x => x.Equals(masterEnum) == false);
        }
        
        if (soundCore.TryGetAudioMixerGroupDic(masterEnum, out var groupDic)) {
            foreach (var (controlType, audioMixerGroup) in groupDic) {
                audioMixerGroupDic.AutoAdd(controlType, audioMixerGroup);
                audioSourceQueueDic.AutoAdd(controlType);
                audioSourcePlayingDic.AutoAdd(controlType);
                maxQueueDic.AutoAdd(controlType, 0);
            }
        }

        if (soundCore.TryGetSoundAssetInfoList(representControlEnum, out var infoList)) {
            foreach (var info in infoList) {
                UpdateSoundAssetInfo(info);
            }
        }
    }

    protected override void UpdateQueue() {
        lock (updateLock) {
            foreach (var pair in audioSourcePlayingDic) {
                for (var i = 0; i < pair.Value.Count; i++) {
                    if (pair.Value.TryDequeue(out var audioSource)) {
                        if (audioSource.isPlaying == false) {
                            audioSourceQueueDic.AutoAdd(pair.Key, audioSource);
                        } else {
                            pair.Value.Enqueue(audioSource);
                        }
                    }
                }
            }
        }
    }

    protected abstract override void UpdateSoundAssetInfo(SoundAssetInfo info);
    
    public abstract override void PlayRefresh();
    public abstract override void PlayOneShot(SoundTrackEvent trackEvent);
    public abstract override void SubmitSoundOrder(NewSoundOrder order);
    
    protected bool TryGetMasterAudioSource(out AudioSource audioSource) => (audioSource = GetMasterAudioSource()) != null;
    protected virtual AudioSource GetMasterAudioSource() => GetAudioSource(masterEnum);
    
    public bool TryGetAudioMixerGroup(Enum type, out AudioMixerGroup mixerGroup) => (mixerGroup = GetAudioMixerGroup(type)) != null;
    public virtual AudioMixerGroup GetAudioMixerGroup(Enum type) => audioMixerGroupDic.TryGetValue(type, out var mixerGroup) ? mixerGroup : null;
    
    protected bool TryGetRepresentAudioSource(out AudioSource audioMixer) => (audioMixer = GetRepresentAudioSource()) != null;
    protected virtual AudioSource GetRepresentAudioSource() => GetAudioSource(representControlEnum);
    
    protected bool TryGetAudioSource(Enum controlType, out AudioSource audioSource) => (audioSource = GetAudioSource(controlType)) != null;

    protected virtual AudioSource GetAudioSource(Enum controlType) {
        lock (updateLock) {
            if (audioSourceDic.TryGetValue(controlType, out var audioSource)) {
                return audioSource;
            }

            if (TryCreateAudioSource(controlType, out audioSource)) {
                audioSourceDic.AutoAdd(controlType, audioSource);
                return audioSource;
            }
        }
        
        Logger.TraceError($"Invalid type || {controlType}");
        return null;
    }
    
    protected bool TryGetQueueAudioSource(Enum controlType, int maxCreateCount, out AudioSource audioSource) {
        audioSource = GetQueueAudioSource(controlType, maxCreateCount);
        return audioSource != null;
    }

    protected virtual AudioSource GetQueueAudioSource(Enum controlType, int maxCreateCount) {
        if (audioMixerGroupDic.ContainsKey(controlType) == false) {
            Logger.TraceError($"{controlType} is Missing {nameof(AudioMixerGroup)}");
            return null;
        }

        lock (updateLock) {
            if (audioSourceQueueDic.TryGetValue(controlType, out var idleQueue) && audioSourcePlayingDic.TryGetValue(controlType, out var playQueue)) {
                if (idleQueue.TryDequeue(out var audioSource)) {
                    playQueue.Enqueue(audioSource);
                    return audioSource;
                }

                if (maxQueueDic.TryGetValue(controlType, out var count) && count < maxCreateCount && TryCreateAudioSource(controlType, out audioSource)) {
                    maxQueueDic.AutoIncreaseAdd(controlType);
                    playQueue.Enqueue(audioSource);
                    return audioSource;
                }
            }
        }

        Logger.TraceLog($"{controlType} || No More Create {nameof(AudioSource)}", Color.red);
        return null;
    }
    
    protected bool TryCreateAudioSource(Enum controlType, out AudioSource audioSource) {
        audioSource = CreateAudioSource(controlType);
        return audioSource != null;
    }

    protected virtual AudioSource CreateAudioSource(Enum controlType) {
        if (audioSourceRootDic.TryGetValue(controlType, out var go) && TryCreateAudioSource(go, controlType, out var audioSource)) {
            return audioSource;
        }
        
        if (soundCore.TryGetSoundRootObject(controlType, out go) && TryCreateAudioSource(go, controlType, out audioSource)) {
            audioSourceRootDic.AutoAdd(controlType, go);
            return audioSource;
        }
        
        Logger.TraceError($"Missing || {controlType} {nameof(AudioMixerGroup)}");
        return null;
    }

    protected bool TryCreateAudioSource(GameObject go, Enum controlType, out AudioSource audioSource) => (audioSource = CreateAudioSource(go, controlType)) != null;

    protected virtual AudioSource CreateAudioSource(GameObject go, Enum controlType) {
        if (audioMixerGroupDic.TryGetValue(controlType, out var mixerGroup)) {
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = mixerGroup;
            audioSource.playOnAwake = false;
            audioSource.mute = isMute;
            return audioSource;
        }
        
        return null;
    }

    public virtual void UnloadAudioClips() {
        lock (updateLock) {
            foreach (var audioSource in audioSourceDic.Values.Where(audioSource => audioSource.IsValidClip())) {
                audioSource.clip.UnloadAudioData();
                audioSource.clip = null;
            }
            
            foreach (var queue in audioSourceQueueDic.Values) {
                queue.Where(audioSource => audioSource.IsValidClip()).ForEach(audioSource => {
                    audioSource.clip.UnloadAudioData();
                    audioSource.clip = null;
                });
            }
            
            foreach (var queue in audioSourcePlayingDic.Values) {
                queue.Where(audioSource => audioSource.IsValidClip()).ForEach(audioSource => {
                    audioSource.clip.UnloadAudioData();
                    audioSource.clip = null;
                });
            }
        }
    }

    public override void SetMute(bool isMute) {
        this.isMute = isMute;
        lock (updateLock) {
            foreach (var audioSource in audioSourceDic.Values.WhereNotNull()) {
                audioSource.mute = isMute;
            }
            
            foreach (var queue in audioSourceQueueDic.Values) {
                foreach (var audioSource in queue) {
                    audioSource.mute = isMute;
                }
            }
            
            foreach (var audioSource in audioSourceQueueDic.Values.SelectMany(queue => queue).Where(audioSource => audioSource != null)) {
                audioSource.mute = isMute;
            }
            
            foreach (var audioSource in audioSourcePlayingDic.Values.SelectMany(queue => queue).Where(audioSource => audioSource != null)) {
                audioSource.mute = isMute;
            }
        }
    }
}