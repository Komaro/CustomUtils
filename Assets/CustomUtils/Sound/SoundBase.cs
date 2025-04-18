using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;

[RequiresAttributeImplementation(typeof(MasterSoundAttribute))]
[RequiresAttributeImplementation(typeof(ControlSoundAttribute))]
public abstract class SoundBase {

    protected SoundCoreBase soundCore;
    protected HashSet<Enum> soundControlSet = new();

    protected Dictionary<Enum, AudioMixerGroup> audioMixerGroupDic = new();
    protected Dictionary<Enum, AudioSource> audioSourceDic = new();

    protected Dictionary<Enum, Queue<AudioSource>> audioSourceQueueDic = new();
    protected Dictionary<Enum, Queue<AudioSource>> audioSourcePlayingDic = new();
    protected Dictionary<Enum, int> maxQueueDic = new();

    private Dictionary<Enum, GameObject> _audioSourceRootDic = new();

    public Enum masterEnum;
    public Enum representControlEnum;
    protected bool isMute;

    protected readonly char PATH_SEPARATOR = '/';
    protected readonly char NAME_SEPARATOR = '_';
    protected readonly char EXTENSION_SEPARATOR = '.';

    public SoundBase(SoundCoreBase soundCore) {
        this.soundCore = soundCore;

        var type = GetType();
        if (type.TryGetCustomAttribute<MasterSoundAttribute>(out var masterAttribute)) {
            masterEnum = masterAttribute.masterEnum;
            representControlEnum = masterAttribute.representControlEnum;

            soundControlSet.Add(masterAttribute.masterEnum);
            soundControlSet.Add(masterAttribute.representControlEnum);
        }

        if (type.TryGetCustomAttribute<ControlSoundAttribute>(out var controlAttribute)) {
            foreach (var controlEnum in controlAttribute.controlList) {
                soundControlSet.Add(controlEnum);
            }
        }
    }
    
    public void Init() {
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

    public virtual void ExtensionDataRefresh(SoundCoreBase soundCore) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void UpdateQueue() {
        lock (audioSourcePlayingDic) {
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

    protected abstract void UpdateSoundAssetInfo(SoundAssetInfo info);
    
    public abstract void PlayRefresh();

    public virtual void PlayOneShot(AudioClip clip) {
        if (clip != null && TryGetRepresentAudioSource(out var audioSource)) {
            audioSource.PlayOneShot(clip);
        }
    }

    public abstract void SubmitSoundOrder(SoundOrder order);

    public bool TryGetAudioMixerGroup(Enum type, out AudioMixerGroup mixerGroup) {
        mixerGroup = GetAudioMixerGroup(type);
        return mixerGroup != null;
    } 
    
    public virtual AudioMixerGroup GetAudioMixerGroup(Enum type) => audioMixerGroupDic.TryGetValue(type, out var mixerGroup) ? mixerGroup : null;

    protected bool TryGetMasterAudioSource(out AudioSource audioSource) {
        audioSource = GetMasterAudioSource();
        return audioSource != null;
    }

    protected virtual AudioSource GetMasterAudioSource() => GetAudioSource(masterEnum);
    
    protected bool TryGetRepresentAudioSource(out AudioSource audioMixer) {
        audioMixer = GetRepresentAudioSource();
        return audioMixer != null;
    }
    
    protected virtual AudioSource GetRepresentAudioSource() => GetAudioSource(representControlEnum);

    protected bool TryGetAudioSource(Enum controlType, out AudioSource audioSource) {
        audioSource = GetAudioSource(controlType);
        return audioSource != null;
    }
    
    protected virtual AudioSource GetAudioSource(Enum controlType) {
        lock (audioSourceDic) {
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

        lock (audioSourceQueueDic) {
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
        if (_audioSourceRootDic.TryGetValue(controlType, out var go) && TryCreateAudioSource(go, controlType, out var audioSource)) {
            return audioSource;
        }
        
        if (soundCore.TryGetSoundRootObject(controlType, out go) && TryCreateAudioSource(go, controlType, out audioSource)) {
            _audioSourceRootDic.AutoAdd(controlType, go);
            return audioSource;
        }
        
        Logger.TraceError($"Missing || {controlType} {nameof(AudioMixerGroup)}");
        return null;
    }

    protected bool TryCreateAudioSource(GameObject go, Enum controlType, out AudioSource audioSource) {
        audioSource = CreateAudioSource(go, controlType);
        return audioSource != null;
    }
    
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
        lock (audioSourceDic) {
            foreach (var audioSource in audioSourceDic.Values.Where(audioSource => audioSource.IsValid())) {
                audioSource.clip.UnloadAudioData();
                audioSource.clip = null;
            }
        }

        lock (audioSourceQueueDic) {
            foreach (var queue in audioSourceQueueDic.Values) {
                queue.Where(audioSource => audioSource.IsValid()).ForEach(audioSource => {
                    audioSource.clip.UnloadAudioData();
                    audioSource.clip = null;
                });
            }
        }

        lock (audioSourcePlayingDic) {
            foreach (var queue in audioSourcePlayingDic.Values) {
                queue.Where(audioSource => audioSource.IsValid()).ForEach(audioSource => {
                    audioSource.clip.UnloadAudioData();
                    audioSource.clip = null;
                });
            }
        }
    }

    public virtual void LoadSystemVolume() => SetVolume(1f);
    public virtual float GetVolume() => soundCore.GetVolume(masterEnum);
    public virtual void SetVolume(float volume) => soundCore.SetVolume(masterEnum, volume);
    
    public virtual bool GetMute() => isMute;
    
    public virtual void SetMute(bool isMute) {
        this.isMute = isMute;
        lock (audioSourceDic) {
            foreach (var audioSource in audioSourceDic.Values.Where(audioSource => audioSource != null)) {
                audioSource.mute = isMute;
            }
        }

        lock (audioSourceQueueDic) {
            foreach (var audioSource in audioSourceQueueDic.Values.SelectMany(queue => queue).Where(audioSource => audioSource != null)) {
                audioSource.mute = isMute;
            }
        }

        lock (audioSourcePlayingDic) {
            foreach (var audioSource in audioSourcePlayingDic.Values.SelectMany(queue => queue).Where(audioSource => audioSource != null)) {
                audioSource.mute = isMute;
            }
        }
    }

    public bool IsContainsControlType(Enum type) => soundControlSet.Contains(type);
}

public abstract record SoundOrder {
    
    public Enum masterType;
    public Enum representControlType;
    
    public SoundOrder(Enum masterType, Enum representControlType) {
        this.masterType = masterType;
        this.representControlType = representControlType;
    }
}