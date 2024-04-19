using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;

[MasterSound(SAMPLE_MASTER_SOUND_TYPE.TYPE_1, SAMPLE_BGM_CONTROL_TYPE.TYPE_1)]
[ControlSound(SAMPLE_MASTER_SOUND_TYPE.TYPE_1, SAMPLE_BGM_CONTROL_TYPE.TYPE_1)]
public class Sample_SoundBgm : SoundBase {

    private SAMPLE_BGM_TYPE _currentBgmType;
    private string _currentBgmOption;
    
    private int _currentMapId;
    
    private Dictionary<SAMPLE_BGM_TYPE, Dictionary<string, string>> _bgmTrackNameDic = new();
    private Dictionary<SAMPLE_BGM_TYPE, Dictionary<string, SoundTrack>> _bgmTrackDic = new();
    
    private Stack<SoundTrack> _waitingStack = new();
    
    private bool _isLock = false;
    
    private const int SAMPLE_MAX_BGM_QUEUE_COUNT = 5;

    public Sample_SoundBgm(SoundCoreBase soundCore) : base(soundCore) {
        Observable.Interval(TimeSpan.FromSeconds(0.5f)).ObserveOnMainThread().Subscribe(_ => {
            lock (_waitingStack) {
                if (_waitingStack.TryPop(out var track)) {
                    PlayBgm(track);
                    _waitingStack.Clear();
                }
            }
            
            UpdateQueue();
        }).AddTo(soundCore);
    }

    protected override void UpdateSoundAssetInfo(SoundAssetInfo info) {
        if (string.IsNullOrEmpty(info.path) || string.IsNullOrEmpty(info.name)) {
            Logger.TraceError($"Invalid {nameof(info)}");
            return;
        }

        var splitName = info.name.Split(NAME_SEPARATOR);
        if (splitName != null && splitName.Length >= 3 && EnumUtil.TryGetValue<SAMPLE_BGM_TYPE>(splitName[1], out var type)) {
            _bgmTrackNameDic.AutoAdd(type, string.Join(NAME_SEPARATOR, splitName, 2, splitName.Length - 2), info.name);
        }
    }

    public override void PlayRefresh() {
        lock (audioSourcePlayingDic) {
            if (audioSourcePlayingDic.TryGetValue(SAMPLE_BGM_CONTROL_TYPE.TYPE_3, out var queue)) {
                if (queue?.Count > 0) {
                    foreach (var audioSource in queue) {
                        // 실제 Android Mobile 환경에서는 작동하지 않음. 블루투스 전환과 함께 timeSamples 또한 0으로 초기화 되는 것으로 보임
                        audioSource.time = audioSource.timeSamples > 0 ? (float)audioSource.timeSamples / audioSource.clip.frequency : 0;
                        audioSource.Play();
                    }
                }
            } else if (_currentBgmType != default) {
                PlayBgm(_currentBgmType, _currentBgmOption);
            }
        }
    }

    public override void SubmitSoundOrder(SoundOrder order) { }

    public void PlayOneShot(SoundTrack track) => PlayBgm(track);

    public void PlayBgm(SAMPLE_BGM_TYPE type, int option) {
        switch (type) {
            case SAMPLE_BGM_TYPE.TYPE_1:
            case SAMPLE_BGM_TYPE.TYPE_2:
            case SAMPLE_BGM_TYPE.TYPE_3:
                PlayBgm(type, $"{option:00}");
                break;
            default:
                PlayBgm(type, option.ToString());
                break;
        }
    }

    public void PlayBgm(SAMPLE_BGM_TYPE type, string option) {
        lock (audioSourcePlayingDic) {
            if (audioSourcePlayingDic.Values.Any(x => x?.Count > 0) && _isLock) {
                return;
            }
        }

        if (string.IsNullOrEmpty(option)) {
            return;
        }
        
        option = option.ToUpper(Constants.Culture.DEFAULT_CULTURE_INFO);
        if (_currentBgmType == type && _currentBgmOption == option && IsEmptyBgmStack() == false) {
            return;
        }
        
        if (_bgmTrackDic.TryGetOrAddValue(type, out var trackDic)) {
            if (trackDic.TryGetValue(option, out var track) == false) {
                if (_bgmTrackNameDic.TryGetValue(type, out var trackNameDic)) {
                    if (trackNameDic.TryGetValue(option, out var trackName) == false && trackNameDic.TryGetRandom(out var randomOption, out var randomTrackName)) {
                        option = randomOption;
                        trackName = randomTrackName;
                    }
                    
                    if (ResourceManager.instance.TryGet(trackName, out track)) {
                        _bgmTrackDic.AutoAdd(type, option, track);
                    } else {
                        Logger.TraceLog($"{nameof(trackDic)} is Empty. Check Sound Track Resource || {type} || {option}", Color.red);
                        return;
                    }
                }
            }

            if (track != null) {
                StopBgm();

                _currentBgmType = type;
                _currentBgmOption = option;
                
                // Frame Delay로 인한 싱크 오류를 방지하기 위해 다음 프레임 끝에서 처리
                Observable.NextFrame(FrameCountType.EndOfFrame).Subscribe(_ => {
                    lock (_waitingStack) {
                        if (track != null) {
                            _waitingStack.Push(track); 
                        }
                    }
                });
            }
        }
    }

    private void PlayBgm(SoundTrack track) {
        if (track == null) {
            Logger.TraceError($"{nameof(track)} is Null");
            return;
        }

        if (track.IsValid(out var error) == false) {
            Logger.TraceWarning($"{nameof(track)} is Invalid || {track.name} || {error} || {error.GetDescription()}");
            return;
        }

        switch (track.type) {
            case E_TRACK_TYPE.OVERLAP:
                if (TryGetAudioSource(SAMPLE_BGM_CONTROL_TYPE.TYPE_2, out var audioSource)) {
                    track.eventList.ForEach(x => audioSource.PlayOneShot(x.clip));
                }
                break;
            case E_TRACK_TYPE.RANDOM:
                if (TryGetAudioSource(SAMPLE_BGM_CONTROL_TYPE.TYPE_2, out audioSource) && track.eventList.TryGetRandom(out var randomTrack)) {
                    audioSource.PlayOneShot(randomTrack.clip);
                }
                break;
            default:
                var startTime = AudioSettings.dspTime + 0.2;
                var delayTime = 0d;
                foreach (var trackEvent in track.eventList) {
                    var playAudioSource = trackEvent.loop ? GetAudioSource(SAMPLE_BGM_CONTROL_TYPE.TYPE_3) : GetAudioSource(SAMPLE_BGM_CONTROL_TYPE.TYPE_2);
                    if (playAudioSource == null && TryGetRepresentAudioSource(out playAudioSource) == false) {
                        Logger.TraceError($"{nameof(playAudioSource)} is Null. {nameof(AudioMixerGroup)} Empty");
                        return;
                    }
                    
                    if (playAudioSource != null) {
                        playAudioSource.Set(trackEvent);
                        playAudioSource.PlayScheduled(startTime + delayTime);
                        if (trackEvent.loop == false) {
                            delayTime += (double)trackEvent.clip.samples / trackEvent.clip.frequency;
                        }
                    }
                }
                break;
        }
    }

    public void StopBgm() {
        lock (audioSourcePlayingDic) {
            _waitingStack.Clear();
            foreach (var audioSource in audioSourcePlayingDic.Values.SelectMany(x => x)) {
                audioSource.Stop();
            }
        }
    }
    
    protected override AudioSource GetAudioSource(Enum type) => TryGetQueueAudioSource(type, SAMPLE_MAX_BGM_QUEUE_COUNT, out var audioSource) ? audioSource : null;

    public override void UnloadAudioClips() {
        StopBgm();
        
        base.UnloadAudioClips();
        
        _bgmTrackDic.SafeClear(trackDic => {
            foreach (var track in trackDic.Values) {
                track.UnloadAudioClip();
            }
        });
    }

    public override void LoadSystemVolume() => SetVolume(1); // Input Local Save Value
    public void SetLock(bool isLock) => _isLock = isLock;

    private bool IsEmptyBgmStack() {
        lock (_waitingStack) {
            return audioSourcePlayingDic.Values.All(x => x?.Count <= 0) && _waitingStack.Count <= 0;
        }
    }
}

public enum SAMPLE_BGM_TYPE {
    NONE,
    TYPE_1,
    TYPE_2,
    TYPE_3,
}
