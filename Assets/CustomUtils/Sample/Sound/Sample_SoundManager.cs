using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;

[Obsolete]
public sealed class Sample_SoundManager : Singleton<Sample_SoundManager> {

    private SoundCoreBase _core;
    private Sample_SoundBgm _bgm;

    private List<SoundBase> _soundList = new();

    private IDisposable _muteDisposable;

    public void AsyncInit(Action callback = null) {
        _core = SoundCoreBase.Create<Sample_SoundCore>(OnChangeAudioConfiguration);
        if (_core != null) {
            Logger.TraceLog($"{nameof(SoundCoreBase)} Activate", Color.cyan);
        }

        MainThreadDispatcher.StartCoroutine(InitSound());
        callback?.Invoke();
    }

    public void AsyncRestartInit(Action callback = null) {
        MainThreadDispatcher.StartCoroutine(RestartInitSound());
        callback?.Invoke();
    }

    private IEnumerator InitSound() {
        _soundList.Clear();
        if (_core != null) {
            var fieldInfoDic = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.FieldType.BaseType == typeof(SoundBase)).ToDictionary(x => x.FieldType, x => x);
            foreach (var soundType in ReflectionProvider.GetSubClassTypes<SoundBase>()) {
                if (fieldInfoDic.TryGetValue(soundType, out var fieldInfo) && fieldInfo.GetValue(this) == null) {
                    var sound = Activator.CreateInstance(soundType, this);
                    if (sound is SoundBase Sample_SoundBase) {
                        fieldInfo.SetValue(this, Sample_SoundBase);
                        _soundList.Add(Sample_SoundBase);
                    }
                }
                
                yield return null;
            }

            // Input Local Save Value
            SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_1);
            SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_2);

            foreach (var sound in _soundList) {
                sound.Init();
                sound.LoadSystemVolume();
                sound.ExtensionDataRefresh(_core);
                Logger.TraceLog($"{sound.GetType().Name} Activate", Color.cyan);
                yield return null;
            }
        }

        yield return null;
    }

    private IEnumerator RestartInitSound() {
        // Input Local Save Value
        SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_1);
        SetVolume(1, SAMPLE_MASTER_SOUND_TYPE.TYPE_2);

        if (_core == null) {
            AsyncInit();
        } else {
            _soundList.ForEach(x => x.ExtensionDataRefresh(_core));
        }

        yield return null;
    }

    #region [Bgm]

    public void PlayBgm(SAMPLE_BGM_TYPE type, int option = 1) {
        if (_bgm == null) {
            Logger.Warning($"{nameof(_bgm)} is Null");
            return;
        }

        _bgm.PlayBgm(type, option);
    }

    public void PlayBgm(SAMPLE_BGM_TYPE type, string option) {
        if (_bgm == null) {
            Logger.Warning($"{nameof(_bgm)} is Null");
            return;
        }

        _bgm.PlayBgm(type, option);
    }

    public void StopBgm() {
        if (_bgm == null) {
            Logger.Warning($"{nameof(_bgm)} is Null");
            return;
        }

        _bgm.StopBgm();
    }

    public void SetLock(bool isLock) {
        if (_bgm == null) {
            Logger.Warning($"{nameof(_bgm)} is Null");
            return;
        }

        _bgm.SetLock(isLock);
    }

    #endregion

    #region [Instant]

    public void PlayOneShot(Enum type, SoundTrack track) {
        if (track != null && track.IsValid() && TryGetMatchSoundExList(out var soundList, type)) {
            var sound = soundList.First();
            switch (track.trackType.Value) {
                case TRACK_TYPE.OVERLAP:
                    track.eventList.ForEach(x => sound.PlayOneShot(x.clip));
                    break;
                case TRACK_TYPE.RANDOM:
                    sound.PlayOneShot(track.eventList.GetRandom().clip);
                    break;
                default:
                    sound.PlayOneShot(track.eventList.First().clip);
                    break;
            }
        }
    }

    #endregion

    /// <param name="volume">0 ~ 1</param>
    /// <param name="type">Control Type</param>
    public void SetVolume(float volume, Enum type) => _core.SetVolume(type, volume);

    /// <summary>
    /// 컨트롤 하는 각 Sample_SoundBase 사운드 볼륨 값을 조정함
    /// </summary>
    public void MuteAllSound(bool isMute, float timeOut = 0) {
        _soundList.ForEach(x => x.SetMute(isMute));
        if (timeOut > 0 && isMute) {
            _muteDisposable?.Dispose();
            _muteDisposable = Observable.Timer(TimeSpan.FromSeconds(timeOut)).Timeout(TimeSpan.FromSeconds(timeOut + 1)).Subscribe(_ => { MuteAllSound(false); }, onError: _ => { MuteAllSound(false); });
        }
    }

    public void MuteSound(bool isMute, params Enum[] types) {
        if (types is not { Length: > 0 }) {
            MuteAllSound(isMute);
            return;
        }

        GetMatchSoundExList(types)?.ForEach(x => x.SetMute(isMute));
    }

    private bool TryGetMatchSoundExList(out List<SoundBase> soundList, params Enum[] enums) {
        soundList = GetMatchSoundExList(enums);
        return soundList is { Count: > 0 };
    }

    private List<SoundBase> GetMatchSoundExList(params Enum[] enums) => _soundList.FindAll(x => enums.Any(x.IsContainsControlType));

    public bool TryGetAudioMixerGroup(Enum type, out AudioMixerGroup mixerGroup) {
        mixerGroup = null;
        return GetMatchSoundExList(type)?.First()?.TryGetAudioMixerGroup(type, out mixerGroup) ?? false;
    }

    public AudioMixerGroup GetAudioMixerGroup(Enum type) => GetMatchSoundExList(type)?.First()?.GetAudioMixerGroup(type);
    public void UnloadAllAudioClip() => _soundList.ForEach(x => x.UnloadAudioClips());
    public void TransitionSnapshot(SAMPLE_SOUND_SNAPSHOT_TYPE type, float transitionTime = 0f) => _core?.TransitionSnapshot(type, transitionTime);

    #region [Task]

    public Task PlayBgmTask(SAMPLE_BGM_TYPE type, string option) {
        PlayBgm(type, option);
        return Task.CompletedTask;
    }

    #endregion

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
public enum SAMPLE_MASTER_SOUND_TYPE {
    TYPE_1,
    TYPE_2,
}

[ControlSoundEnumType(SAMPLE_MASTER_SOUND_TYPE.TYPE_1)]
public enum SAMPLE_BGM_CONTROL_TYPE {
    TYPE_1,
    TYPE_2,
    TYPE_3,
}

[SnapshotEnumType]
public enum SAMPLE_SOUND_SNAPSHOT_TYPE {
    TYPE_1,
    TYPE_2,
    TYPE_3,
}