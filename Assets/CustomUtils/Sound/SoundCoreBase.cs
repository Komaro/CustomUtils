using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

public abstract class SoundCoreBase : MonoBehaviour {
    
    private HashSet<Enum> _cacheSnapshotEnumSet = new();
    private Dictionary<Enum, List<SoundAssetInfo>> _cacheSoundInfoDic = new();
    private Dictionary<string, Enum> _cacheSoundEnumDic = new();

    private AudioMixer _audioMixer;

    private Dictionary<Enum, Dictionary<Enum, AudioMixerGroup>> _mixerGroupDic = new();
    private Dictionary<Enum, GameObject> _soundRootDic = new();

    private Enum _currentSnapshotType;

    private bool _isInit;

    private static Action<bool> _onChangeAudioConfigurationCallback;

    private const float DEFAULT_MIN_SOUND_VALUE = 0.0001f;
    private const float DEFAULT_MIN_VOLUME = -80f;

    public static SoundCoreBase Create<T>(Action<bool> onChangeAudioConfigurationCallback = null) where T : SoundCoreBase {
        _onChangeAudioConfigurationCallback = onChangeAudioConfigurationCallback;
        var go = new GameObject(typeof(T).Name);
        return go.AddComponent<T>();
    }

    private void Awake() {
        DontDestroyOnLoad(gameObject);

        _audioMixer = LoadAudioMixer();
        if (_audioMixer == null) {
            Logger.TraceError($"Missing {nameof(_audioMixer)}. Check {nameof(LoadAudioMixer)} Method");
            return;
        }

        CreateSoundObject();

        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;

        _isInit = true;

        CacheSoundEnum();
        CacheSnapshotEnum();
        CacheSoundInfo();
    }

    private void OnDestroy() {
        _audioMixer = null;

        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
    }

    protected virtual void CreateSoundObject() {
        if (TryGetEnumList<MasterSoundEnumTypeAttribute>(out var masterEnumList) && TryGetEnumList<ControlSoundEnumTypeAttribute>(out var controlEnumList)) {
            var controlEnumDic = controlEnumList.GroupBy(x => x.GetType().GetCustomAttribute<ControlSoundEnumTypeAttribute>().masterType, x => x).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var masterEnum in masterEnumList.Where(masterEnum => _soundRootDic.ContainsKey(masterEnum) == false)) {
                CreateRootObject(masterEnum);
                var mixerGroupDic = _audioMixer.FindMatchingGroups(masterEnum.ToString()).ToDictionary(x => x.name, x => x);
                if (mixerGroupDic is { Count: > 0 } && controlEnumDic.TryGetValue(masterEnum, out controlEnumList)) {
                    if (mixerGroupDic.TryGetValue(masterEnum.ToString(), out var mixerGroup)) {
                        _mixerGroupDic.AutoAdd(masterEnum, masterEnum, mixerGroup);
                    }
                    
                    foreach (var controlEnum in controlEnumList) {
                        if (mixerGroupDic.TryGetValue(controlEnum.ToString(), out mixerGroup)) {
                            _mixerGroupDic.AutoAdd(masterEnum, controlEnum, mixerGroup);
                            CreateRootObject(masterEnum, controlEnum);
                        }
                    }
                }
            }
        }
    }

    protected virtual void CacheSoundEnum() {
        _cacheSoundEnumDic.Clear();
        foreach (var enumKey in _soundRootDic.Keys) {
            _cacheSoundEnumDic.AutoAdd(enumKey.ToString(), enumKey);
        }
    }

    protected virtual void CacheSnapshotEnum() {
        _cacheSnapshotEnumSet.Clear();
        if (TryGetEnumList<SnapshotEnumTypeAttribute>(out var snapshotList)) {
            foreach (var snapshotEnum in snapshotList) {
                _cacheSnapshotEnumSet.Add(snapshotEnum);
            }
        }
    }

    protected virtual void CacheSoundInfo() {
        _cacheSoundInfoDic.Clear();
        var infoList = LoadSoundAssetInfoList();
        if (infoList != null) {
            foreach (var info in infoList) {
                var splits = info.path.Split('/');
                if (splits.Length > 2) {
                    if (_cacheSoundEnumDic.TryGetValue(splits[1], out var type)) {
                        _cacheSoundInfoDic.AutoAdd(type, info);
                    }
                }
            }
        }
    }

    public void RefreshSnapshot() => TransitionSnapshot(_currentSnapshotType, 0f);

    public virtual void TransitionSnapshot(Enum type, float transitionTime) {
        if (_cacheSnapshotEnumSet.Contains(type)) {
            _currentSnapshotType = type;
            _audioMixer?.FindSnapshot(type.ToString())?.TransitionTo(transitionTime);
        }
    }

    public void TransitionSnapshot(string transitionName, float transitionTime) => _audioMixer?.FindSnapshot(transitionName)?.TransitionTo(transitionTime);

    public float GetVolume(Enum type) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is Null");
            return 1f;
        }
        
        _audioMixer.GetFloat(type.ToString(), out var volume);
        return GetAudioMixerVolume2OptionSoundValue(volume);
    }
    
    public virtual void SetVolume(Enum type, float volume) {
        if (_audioMixer == null) {
            Logger.TraceError($"{nameof(_audioMixer)} is Null");
            return;
        }

        _audioMixer.SetFloat(type.ToString(), GetOptionSoundValue2AudioMixerVolume(volume));
    }

    private void CreateRootObject(Enum masterEnum) => CreateRootObject(masterEnum, masterEnum.ToString(), transform);

    private void CreateRootObject(Enum masterEnum, Enum controlEnum) {
        if (_soundRootDic.ContainsKey(masterEnum) == false) {
            Logger.TraceError($"{nameof(masterEnum)} is Missing.");
            return;
        }

        CreateRootObject(controlEnum, controlEnum.ToString(), _soundRootDic[masterEnum].transform);
    }

    protected virtual void CreateRootObject(Enum rootEnum, string name, Transform parent) {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        _soundRootDic.AutoAdd(rootEnum, go);
    }

    protected bool TryGetEnumList<T>(out List<Enum> enumList) where T : Attribute {
        enumList = GetEnumList<T>();
        return enumList is { Count: > 0 };
    }

    protected virtual List<Enum> GetEnumList<T>() where T : Attribute => ReflectionManager.TryGetAttributeEnumTypes<T>(out var types) ? types.SelectMany(x => Enum.GetValues(x).Cast<Enum>()).ToList() : null;

    public bool TryGetAudioMixerGroupDic(Enum type, out Dictionary<Enum, AudioMixerGroup> groupDic) {
        groupDic = GetAudioMixerGroupDic(type);
        return groupDic is { Count: > 0 };
    }

    public Dictionary<Enum, AudioMixerGroup> GetAudioMixerGroupDic(Enum type) => _mixerGroupDic.TryGetValue(type, out var groupDic) ? groupDic : null;

    public bool TryGetSoundRootObject(Enum type, out GameObject go) {
        go = GetSoundRootObject(type);
        return go != null;
    }

    public GameObject GetSoundRootObject(Enum type) => _soundRootDic.TryGetValue(type, out var go) ? go : null;

    public bool TryGetSoundAssetInfoList(Enum type, out List<SoundAssetInfo> list) {
        list = GetSoundAssetInfoList(type);
        return list != null && list.Count > 0;
    }

    public List<SoundAssetInfo> GetSoundAssetInfoList(Enum type) => _cacheSoundInfoDic.TryGetValue(type, out var list) ? list : null;
    
    private float GetOptionSoundValue2AudioMixerVolume(float value) => value < DEFAULT_MIN_SOUND_VALUE ? DEFAULT_MIN_VOLUME : 20 * Mathf.Log10(value);
    private float GetAudioMixerVolume2OptionSoundValue(float value) => value <= DEFAULT_MIN_VOLUME ? DEFAULT_MIN_SOUND_VALUE : Mathf.Pow(10, value / 20);

    protected abstract AudioMixer LoadAudioMixer();
    protected abstract List<SoundAssetInfo> LoadSoundAssetInfoList();

    public virtual bool IsInitialize() => _isInit;

    private void OnAudioConfigurationChanged(bool isChanged) {
        Logger.TraceLog($"Audio Configuration Changed || {isChanged}");
        _onChangeAudioConfigurationCallback?.Invoke(isChanged);
    }
}

public record SoundAssetInfo {

    public string path;
    public string name;

    public SoundAssetInfo(string path, string name) {
        this.path = path;
        this.name = name;
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class MasterSoundEnumTypeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Enum)]
public class ControlSoundEnumTypeAttribute : Attribute {
    
    public readonly Enum masterType;

    public ControlSoundEnumTypeAttribute(object masterType) {
        if (masterType is Enum enumType) {
            this.masterType = enumType;
        }
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class SnapshotEnumTypeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class MasterSoundAttribute : Attribute {
    
    public readonly Enum masterEnum;
    public readonly Enum representControlEnum;

    public MasterSoundAttribute(object masterType, object representControlType) {
        if (masterType is Enum masterEnum) {
            this.masterEnum = masterEnum;
        }

        if (representControlType is Enum representControlEnum) {
            this.representControlEnum = representControlEnum;
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class ControlSoundAttribute : Attribute {
    
    public readonly List<Enum> controlList;

    public ControlSoundAttribute(params object[] controlTypes) => controlList = controlTypes.ConvertTo(x => x as Enum);
}
