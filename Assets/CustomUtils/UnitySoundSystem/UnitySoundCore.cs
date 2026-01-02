using System;
using System.Collections.Concurrent;
using CustomUtils.Sound.NewSoundSystem;
using UnityEngine;
using UnityEngine.Audio;

[TestRequired]
public abstract class UnitySoundCore : CustomUtils.Sound.NewSoundSystem.SoundCoreBase {
    
    private AudioMixer _audioMixer;

    protected readonly MultiLayerConcurrentDictionary<SoundType, SoundType, AudioMixerGroup> mixerGroupDic = new();
    protected readonly GlobalEnum<SnapshotEnumTypeAttribute> snapshotEnum = new();
    
    private const float DEFAULT_MIN_SOUND_VALUE = 0.0001f;
    private const float DEFAULT_MIN_VOLUME = -80f;
    
    protected override void OnAwake() {
        _audioMixer = LoadAudioMixer();
        _audioMixer.ThrowIfUnexpectedNull(nameof(_audioMixer));
        
        AttachAudioMixerGroups();
        
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
    }
    
    public override void Dispose() {
        Destroy(_audioMixer);
        _audioMixer = null;

        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
    }

    protected abstract AudioMixer LoadAudioMixer();

    protected virtual void AttachAudioMixerGroups() {
        mixerGroupDic.Clear();
        foreach (var (masterType, controlTypes) in controlTypeDic) {
            var groupDic = _audioMixer.FindMatchingGroups(masterType.ToString()).ToDictionary(group => group.name);
            if (groupDic == null || groupDic.IsEmpty()) {
                Logger.TraceLog($"{nameof(groupDic)} is null or empty", Color.red);
                continue;
            }
            
            if (groupDic.TryGetValue(masterType.Name, out var mixerGroup)) {
                mixerGroupDic.Add(masterType, masterType, mixerGroup);
            }
            
            foreach (var controlType in controlTypes) {
                if (groupDic.TryGetValue(controlType.Name, out mixerGroup)) {
                    mixerGroupDic.Add(masterType, controlType, mixerGroup);
                }
            }
        }
    }

    public virtual void RefreshSnapshot() => TransitionSnapshot(snapshotEnum.Value, 0f);
    
    public virtual void TransitionSnapshot(Enum snapshotType, float transitionTime) {
        _audioMixer.ThrowIfNull(nameof(_audioMixer));
        if (snapshotEnum.Contains(snapshotType)) {
            snapshotEnum.Set(snapshotType);
            _audioMixer.FindSnapshot(snapshotType.ToString())?.TransitionTo(transitionTime);
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

    public bool TryGetAudioMixerGroupDic(Enum masterType, out ConcurrentDictionary<SoundType, AudioMixerGroup> groupDic) => (groupDic = GetAudioMixerGroupDic(masterType)) is { Count: > 0 };
    public virtual ConcurrentDictionary<SoundType, AudioMixerGroup> GetAudioMixerGroupDic(Enum enumValue) => soundTypeDic.TryGetValue(enumValue, out var soundType) ? GetAudioMixerGroupDic(soundType) : null;

    public bool TryGetAudioMixerGroupDic(SoundType soundType, out ConcurrentDictionary<SoundType, AudioMixerGroup> groupDic) => (groupDic = GetAudioMixerGroupDic(soundType)) is { Count: > 0 };
    public virtual ConcurrentDictionary<SoundType, AudioMixerGroup> GetAudioMixerGroupDic(SoundType soundType) => mixerGroupDic.TryGetValue(soundType, out var groupDic) ? groupDic : null;

    private float GetAudioMixerVolume2OptionSoundValue(float value) => value <= DEFAULT_MIN_VOLUME ? DEFAULT_MIN_SOUND_VALUE : Mathf.Pow(10, value / 20);
    private float GetOptionSoundValue2AudioMixerVolume(float value) => value < DEFAULT_MIN_SOUND_VALUE ? DEFAULT_MIN_VOLUME : 20 * Mathf.Log10(value);
}