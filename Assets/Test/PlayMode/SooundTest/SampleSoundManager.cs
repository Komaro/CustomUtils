using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SoundOrder Test SoundManager
/// </summary>
public class SampleSoundManager : Singleton<SampleSoundManager> {
    
    private SoundCoreBase core;
    private List<SoundBase> soundList = new();
    private Dictionary<Enum, Dictionary<Enum, SoundBase>> soundDic = new();

    private IDisposable muteDisposable;

    public SampleSoundManager() {
        core = SoundCoreBase.Create<SampleSoundCore>(OnChangeAudioConfiguration);
        if (core != null) {
            Logger.TraceLog($"{nameof(SoundCoreBase)} Activate", Color.cyan);
        }

        InitSound();
    }

    private void InitSound() {
        if (core != null) {
            foreach (var soundType in ReflectionProvider.GetSubTypesOfType<SoundBase>()) {
                var sound = Activator.CreateInstance(soundType, (object) core);
                if (sound is SoundBase soundBase) {
                    soundList.Add(soundBase);
                }
            }

            foreach (var sound in soundList) {
                sound.Init();
                sound.LoadSystemVolume();
                sound.ExtensionDataRefresh(core);
                soundDic.AutoAdd(sound.masterEnum, sound.representControlEnum, sound);
                Logger.TraceLog($"{sound.GetType().Name} Activate", Color.cyan);
            }
        }
    }

    internal void Test() {
        
    }
    
    public void SubmitSoundOrder(SoundOrder soundOrder) {
        if (TryGetMatchSound(soundOrder, out var sound)) {
            sound.SubmitSoundOrder(soundOrder);
        }
    }

    /// <param name="volume">0 ~ 1</param>
    /// <param name="type">Control Type</param>
    public void SetVolume(float volume, Enum type) => core.SetVolume(type, volume);

    /// <summary>
    /// 컨트롤 하는 각 Sample_SoundBase 사운드 볼륨 값을 조정함
    /// </summary>
    public void MuteAllSound(bool isMute, float timeOut = 0) {
        soundList.ForEach(x => x.SetMute(isMute));
        if (timeOut > 0 && isMute) {
            muteDisposable?.Dispose();
            muteDisposable = Observable.Timer(TimeSpan.FromSeconds(timeOut)).Timeout(TimeSpan.FromSeconds(timeOut + 1))
                .Subscribe(_ => { MuteAllSound(false); }, onError: _ => { MuteAllSound(false); });
        }
    }

    public void MuteSound(bool isMute, params Enum[] types) {
        if (types is not { Length: > 0 }) {
            MuteAllSound(isMute);
            return;
        }

        GetMatchSoundList(types)?.ForEach(x => x.SetMute(isMute));
    }

    private bool TryGetMatchSound(SoundOrder soundOrder, out SoundBase soundBase) {
        soundBase = GetMatchSound(soundOrder);
        return soundBase != null;
    }

    private SoundBase GetMatchSound(SoundOrder soundOrder) => soundDic.TryGetValue(soundOrder.masterType, soundOrder.representControlType, out var soundBase) ? soundBase : null;

    protected bool TryGetMatchSound(Enum masterType, Enum representType, out SoundBase soundBase) {
        soundBase = GetMatchSound(masterType, representType);
        return soundBase != null;
    }
    
    private SoundBase GetMatchSound(Enum masterType, Enum representTyp) => soundDic.TryGetValue(masterType, representTyp, out var soundBase) ? soundBase : null;

    protected bool TryGetMatchSoundList(out List<SoundBase> soundList, params Enum[] enums) {
        soundList = GetMatchSoundList(enums);
        return soundList is { Count: > 0 };
    }

    private List<SoundBase> GetMatchSoundList(params Enum[] enums) => soundList.FindAll(x => enums.Any(x.IsContainsControlType));

    public bool TryGetAudioMixerGroup(Enum type, out AudioMixerGroup mixerGroup) {
        mixerGroup = null;
        return GetMatchSoundList(type)?.First()?.TryGetAudioMixerGroup(type, out mixerGroup) ?? false;
    }

    public AudioMixerGroup GetAudioMixerGroup(Enum type) => GetMatchSoundList(type)?.First()?.GetAudioMixerGroup(type);
    public void UnloadAllAudioClip() => soundList.ForEach(x => x.UnloadAudioClips());
    
    public void TransitionSnapshot(SAMPLE_SOUND_SNAPSHOT_TYPE type, float transitionTime = 0f) {
        if (core != null) {
            core.TransitionSnapshot(type, transitionTime);
        }
    }

    private void OnChangeAudioConfiguration(bool isChanged) {
#if UNITY_EDITOR == false && UNITY_ANDROID
        _core?.RefreshSnapshot();

        // Input Local Save Value
        SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_1);
        SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_2);
        
        _soundList.ForEach(x => x.PlayRefresh());
#endif
    }
}

[MasterSoundEnumType]
public enum TEST_MASTER_SOUND_TYPE {
    Master,
    MASTER_TEST,
}

[ControlSoundEnumType(TEST_MASTER_SOUND_TYPE.Master)]
public enum TEST_CONTROL_SOUND_TYPE {
    TEST,
}