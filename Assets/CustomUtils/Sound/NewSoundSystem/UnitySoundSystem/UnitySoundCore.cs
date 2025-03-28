using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

public abstract class UnitySoundCore : NewSoundCoreBase {
    
    private AudioMixer _audioMixer;

    // private Dictionary<Enum, Dictionary<Enum, AudioMixerGroup>> _mixerGroupDic = new();
    protected ConcurrentDictionary<Enum, ConcurrentDictionary<Enum, AudioMixerGroup>> mixerGroupDic = new();
    
    
    protected ImmutableHashSet<Enum> cacheSnapshotEnumSet = ImmutableHashSet<Enum>.Empty;
    
    protected Enum currentSnapshotType;
    
    private const float DEFAULT_MIN_SOUND_VALUE = 0.0001f;
    private const float DEFAULT_MIN_VOLUME = -80f;
    
    protected override void OnAwake() {
        _audioMixer = LoadAudioMixer();
        if (_audioMixer == null) {
            Logger.TraceError($"Missing {nameof(_audioMixer)}. Check {nameof(LoadAudioMixer)} Method");
            return;
        }
        
        
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
    }
    
    public override void Dispose() {
        Destroy(_audioMixer);
        _audioMixer = null;

        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
    }

    protected abstract AudioMixer LoadAudioMixer();

    protected override void CreateSoundObject() {
        foreach (var masterEnum in GetEnums<MasterSoundEnumTypeAttribute>().Where(masterEnum => _soundRootDic.ContainsKey(masterEnum) == false)) {
            CreateRootObject(masterEnum);
            var audioMixerGroupDic = _audioMixer.FindMatchingGroups(masterEnum.ToString()).ToDictionary(mixerGroup => mixerGroup.name, mixerGroup => mixerGroup);
            if (controlEnumDic.TryGetValue(masterEnum, out var controlEnumGroup)) {
                if (audioMixerGroupDic.TryGetValue(masterEnum.ToString(), out var mixerGroup)) {
                    mixerGroupDic.AutoAdd(masterEnum, masterEnum, mixerGroup);
                }
                
                foreach (var controlEnum in controlEnumGroup) {
                    if (audioMixerGroupDic.TryGetValue(controlEnum.ToString(), out mixerGroup)) {
                        mixerGroupDic.AutoAdd(masterEnum, controlEnum, mixerGroup);
                    }
                }
            }
        }
        
        
        if (TryGetEnumList<MasterSoundEnumTypeAttribute>(out var masterEnumList) && TryGetEnumList<ControlSoundEnumTypeAttribute>(out var controlEnumList)) {
            var controlEnumDic = controlEnumList.GroupBy(x => x.GetType().GetCustomAttribute<ControlSoundEnumTypeAttribute>().masterType, x => x).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var masterEnum in masterEnumList.Where(masterEnum => _soundRootDic.ContainsKey(masterEnum) == false)) {
                CreateRootObject(masterEnum);
                var mixerGroupDic = _audioMixer.FindMatchingGroups(masterEnum.ToString()).ToDictionary(x => x.name, x => x);
                if (mixerGroupDic is { Count: > 0 } && controlEnumDic.TryGetValue(masterEnum, out controlEnumList)) {
                    if (mixerGroupDic.TryGetValue(masterEnum.ToString(), out var mixerGroup)) {
                        this.mixerGroupDic.AutoAdd(masterEnum, masterEnum, mixerGroup);
                    }
                    
                    foreach (var controlEnum in controlEnumList) {
                        if (mixerGroupDic.TryGetValue(controlEnum.ToString(), out mixerGroup)) {
                            this.mixerGroupDic.AutoAdd(masterEnum, controlEnum, mixerGroup);
                            CreateRootObject(masterEnum, controlEnum);
                        }
                    }
                }
            }
        }
    }
    
    protected virtual void CacheSnapshotEnum() {
        cacheSnapshotEnumSet.Clear();
        if (TryGetEnumList<SnapshotEnumTypeAttribute>(out var snapshotList)) {
            foreach (var snapshotEnum in snapshotList) {
                cacheSnapshotEnumSet.Add(snapshotEnum);
            }
        }
    }


    public override void TransitionSnapshot(Enum type, float transitionTime) {
        if (cacheSnapshotEnumSet.Contains(type)) {
            currentSnapshotType = type;
            if (_audioMixer != null) {
                _audioMixer.FindSnapshot(type.ToString())?.TransitionTo(transitionTime);
            }
        }
    }

    public override void TransitionSnapshot(string transitionName, float transitionTime) {
        if (_audioMixer != null) {
            _audioMixer.FindSnapshot(transitionName)?.TransitionTo(transitionTime);
        }
    }

    public override float GetVolume(Enum type) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is null");
            return 1f;
        }
        
        return _audioMixer.GetFloat(type.ToString(), out var volume) ? GetAudioMixerVolume2OptionSoundValue(volume) : 1f;
    }

    public override float GetVolume<T>(T type) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is null");
            return 1f;
        }

        return _audioMixer.GetFloat(type.ToString(), out var volume) ? GetAudioMixerVolume2OptionSoundValue(volume) : 1f;
    }
    
    public override bool SetVolume(Enum type, float volume) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is null");
            return false;
        }
    
        return _audioMixer.SetFloat(type.ToString(), GetOptionSoundValue2AudioMixerVolume(volume));
    }

    public override bool SetVolume<T>(T type, float volume) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is null");
            return false;
        }

        return _audioMixer.SetFloat(type.ToString(), GetOptionSoundValue2AudioMixerVolume(volume));
    }

    public bool TryGetAudioMixerGroupDic(Enum masterType, out ConcurrentDictionary<Enum, AudioMixerGroup> groupDic) => (groupDic = GetAudioMixerGroupDic(masterType)) is { Count: > 0 };
    public virtual ConcurrentDictionary<Enum, AudioMixerGroup> GetAudioMixerGroupDic(Enum masterType) => mixerGroupDic.TryGetValue(masterType, out var groupDic) ? groupDic : null;

    private float GetOptionSoundValue2AudioMixerVolume(float value) => value < DEFAULT_MIN_SOUND_VALUE ? DEFAULT_MIN_VOLUME : 20 * Mathf.Log10(value);
    private float GetAudioMixerVolume2OptionSoundValue(float value) => value <= DEFAULT_MIN_VOLUME ? DEFAULT_MIN_SOUND_VALUE : Mathf.Pow(10, value / 20);
}