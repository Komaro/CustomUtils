using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

public abstract record SoundType(Enum Value) {
    
    public Enum Value { get; } = Value;
    
    public static implicit operator Enum(SoundType soundType) => soundType.Value;
}

public record MasterType(Enum Value) : SoundType(Value) {

    public static implicit operator MasterType(Enum enumValue) => new(enumValue);
}

public record ControlType(Enum Value) : SoundType(Value) {
    
    public static implicit operator ControlType(Enum enumValue) => new(enumValue);
}



public abstract class NewSoundCoreBase : MonoBehaviour, IDisposable {
    
    // [Obsolete]
    // protected HashSet<Enum> _cacheSnapshotEnumSet = new(); // UnitySoundCore로 이관 
    // private Dictionary<Enum, List<SoundAssetInfo>> _cacheSoundInfoDic = new();
    // private Dictionary<string, Enum> _cacheSoundEnumDic = new();
    
    protected ImmutableHashSet<MasterType> masterTypeSet = ImmutableHashSet<MasterType>.Empty;
    protected ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>> controlTypeDic = ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>>.Empty;

    protected ImmutableHashSet<Enum> masterEnumSet = ImmutableHashSet<Enum>.Empty;
    protected ImmutableDictionary<Enum, ImmutableHashSet<Enum>> controlEnumDic = ImmutableDictionary<Enum, ImmutableHashSet<Enum>>.Empty;
    
    // protected MultiLevelDictionary<>

    protected ConcurrentDictionary<Enum, List<SoundAssetInfo>> soundInfoDic = new();
    
    protected ConcurrentDictionary<string, Enum> soundEnumDic = new();

    // private AudioMixer _audioMixer;
    //
    // private Dictionary<Enum, Dictionary<Enum, AudioMixerGroup>> _mixerGroupDic = new();
    
    // private Dictionary<Enum, GameObject> _soundRootDic = new();
    protected Dictionary<Enum, GameObject> _soundRootDic = new();

    [Obsolete]
    protected Enum _currentSnapshotType;

    protected bool _isInit;

    protected static Action<bool> _onChangeAudioConfigurationCallback;

    // protected const float DEFAULT_MIN_SOUND_VALUE = 0.0001f;
    // protected const float DEFAULT_MIN_VOLUME = -80f;

    public static SoundCoreBase Create<T>(Action<bool> onChangeAudioConfigurationCallback = null) where T : SoundCoreBase {
        _onChangeAudioConfigurationCallback = onChangeAudioConfigurationCallback;
        var go = new GameObject(typeof(T).Name);
        return go.AddComponent<T>();
    }

    private void Awake() {
        DontDestroyOnLoad(gameObject);

        OnAwake();
        // _audioMixer = LoadAudioMixer();
        // if (_audioMixer == null) {
        //     Logger.TraceError($"Missing {nameof(_audioMixer)}. Check {nameof(LoadAudioMixer)} Method");
        //     return;
        // }
        
        // 1. enum cache
        masterTypeSet = LoadMasterTypeSet();
        
        masterEnumSet = GetMasterEnumTypeSet();
        controlEnumDic = GetControlEnumTypeDic();
        
        // 2. info cache
        soundInfoDic = GetSoundInfoDic();

        // 3. create object
        //      1) Get create sound object tree
        //      2) Create object tree
        CreateSoundObject();

        // AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;


        // CacheSoundEnum();
        // CacheSnapshotEnum(); // UnitySoundCore로 이관
        // CacheSoundInfo();
        
        _isInit = true;
    }

    protected abstract void OnAwake();

    private void OnDestroy() {
        // _audioMixer = null;
        Dispose();

        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
    }

    public abstract void Dispose();

    protected virtual ImmutableHashSet<MasterType> LoadMasterTypeSet() => GetEnums<MasterSoundEnumTypeAttribute>().Select(enumValue => new MasterType(enumValue)).ToImmutableHashSetWithDistinct();

    protected virtual ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>> LoadControlType() {
        // return ReflectionProvider.GetAttributeEnumInfos<ControlSoundEnumTypeAttribute>()
        //     .GroupBy(info => info.attribute.masterType, info => info.enumType)
        //     .ToDictionary(grouping => new MasterType(grouping.Key), grouping => grouping.SelectMany(type => EnumUtil.GetValues(type)).ToImmutableHashSetWithDistinct())
        //     .ToImmutableDictionary();
        
        
        return ReflectionProvider.GetAttributeEnumInfos<ControlSoundEnumTypeAttribute>()
            .GroupBy(info => info.attribute.masterType, info => info.enumType)
            .ToImmutableDictionary(grouping => new MasterType(grouping.Key),
                grouping => grouping.SelectMany(type => EnumUtil.GetValues(type).Select(enumValue => new ControlType(enumValue))).ToImmutableHashSetWithDistinct());
    }

    [Obsolete]
    protected virtual ImmutableHashSet<Enum> GetMasterEnumTypeSet() => GetEnums<MasterSoundEnumTypeAttribute>().ToImmutableHashSetWithDistinct();
    
    [Obsolete("Wrong Return")]
    protected virtual ImmutableDictionary<Enum, ImmutableHashSet<Enum>> GetControlEnumTypeDic() => GetEnums<ControlSoundEnumTypeAttribute>().GroupBy(enumValue => enumValue, enumValue => enumValue).ToImmutableDictionary(grouping => grouping.Key, grouping => grouping.ToImmutableHashSetWithDistinct());


    protected abstract List<SoundAssetInfo> LoadSoundAssetInfoList();
    public virtual bool IsInitialize() => _isInit;
    
    protected abstract void CreateSoundObject();
    // protected virtual void CreateSoundObject() {
    //     if (TryGetEnumList<MasterSoundEnumTypeAttribute>(out var masterEnumList) && TryGetEnumList<ControlSoundEnumTypeAttribute>(out var controlEnumList)) {
    //         var controlEnumDic = controlEnumList.GroupBy(x => x.GetType().GetCustomAttribute<ControlSoundEnumTypeAttribute>().masterType, x => x).ToDictionary(x => x.Key, x => x.ToList());
    //         foreach (var masterEnum in masterEnumList.Where(masterEnum => _soundRootDic.ContainsKey(masterEnum) == false)) {
    //             CreateRootObject(masterEnum);
    //             var mixerGroupDic = _audioMixer.FindMatchingGroups(masterEnum.ToString()).ToDictionary(x => x.name, x => x);
    //             if (mixerGroupDic is { Count: > 0 } && controlEnumDic.TryGetValue(masterEnum, out controlEnumList)) {
    //                 if (mixerGroupDic.TryGetValue(masterEnum.ToString(), out var mixerGroup)) {
    //                     _mixerGroupDic.AutoAdd(masterEnum, masterEnum, mixerGroup);
    //                 }
    //                 
    //                 foreach (var controlEnum in controlEnumList) {
    //                     if (mixerGroupDic.TryGetValue(controlEnum.ToString(), out mixerGroup)) {
    //                         _mixerGroupDic.AutoAdd(masterEnum, controlEnum, mixerGroup);
    //                         CreateRootObject(masterEnum, controlEnum);
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }

    protected virtual void CacheSoundEnum() {
        soundEnumDic.Clear();
        foreach (var enumKey in _soundRootDic.Keys) {
            soundEnumDic.AutoAdd(enumKey.ToString(), enumKey);
        }
    }

    // protected virtual void CacheSnapshotEnum() {
    //     _cacheSnapshotEnumSet.Clear();
    //     if (TryGetEnumList<SnapshotEnumTypeAttribute>(out var snapshotList)) {
    //         foreach (var snapshotEnum in snapshotList) {
    //             _cacheSnapshotEnumSet.Add(snapshotEnum);
    //         }
    //     }
    // }

    protected abstract ConcurrentDictionary<Enum, List<SoundAssetInfo>> GetSoundInfoDic();
    
    [Obsolete]
    protected virtual void CacheSoundInfo() {
        soundInfoDic.Clear();
        var infoList = LoadSoundAssetInfoList();
        if (infoList != null) {
            foreach (var info in infoList) {
                var splits = info.path.Split('/');
                if (splits.Length > 2) {
                    if (soundEnumDic.TryGetValue(splits[1], out var type)) {
                        soundInfoDic.AutoAdd(type, info);
                    }
                }
            }
        }
    }

    public void RefreshSnapshot() => TransitionSnapshot(_currentSnapshotType, 0f);

    public abstract void TransitionSnapshot(Enum type, float transitionTime);
    // public virtual void TransitionSnapshot(Enum type, float transitionTime) {
    //     if (_cacheSnapshotEnumSet.Contains(type)) {
    //         _currentSnapshotType = type;
    //         if (_audioMixer != null) {
    //             _audioMixer.FindSnapshot(type.ToString())?.TransitionTo(transitionTime);
    //         }
    //     }
    // }

    public abstract void TransitionSnapshot(string transitionName, float transitionTime);
    // public void TransitionSnapshot(string transitionName, float transitionTime) {
    //     if (_audioMixer != null) {
    //         _audioMixer.FindSnapshot(transitionName)?.TransitionTo(transitionTime);
    //     }
    // }

    public abstract float GetVolume(Enum type);
    // public float GetVolume(Enum type) {
    //     if (_audioMixer == null) {
    //         Logger.TraceError($"{nameof(_audioMixer)} is Null");
    //         return 1f;
    //     }
    //     
    //     _audioMixer.GetFloat(type.ToString(), out var volume);
    //     return GetAudioMixerVolume2OptionSoundValue(volume);
    // }
    
    public abstract float GetVolume<T>(T type) where T : struct, Enum;

    public abstract bool SetVolume(Enum type, float volume);
    // public virtual void SetVolume(Enum type, float volume) {
    //     if (_audioMixer == null) {
    //         Logger.TraceError($"{nameof(_audioMixer)} is Null");
    //         return;
    //     }
    //
    //     _audioMixer.SetFloat(type.ToString(), GetOptionSoundValue2AudioMixerVolume(volume));
    // }

    public abstract bool SetVolume<T>(T type, float volume) where T : struct, Enum; 

    protected void CreateRootObject(Enum masterEnum) => CreateRootObject(masterEnum, masterEnum.ToString(), transform);

    protected void CreateRootObject(Enum masterEnum, Enum controlEnum) {
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

    [Obsolete("Will be replaced with TryGetEnums")]
    protected bool TryGetEnumList<T>(out List<Enum> enumList) where T : Attribute {
        enumList = GetEnumList<T>();
        return enumList is { Count: > 0 };
    }

    [Obsolete("Will be replaced with GetEnums")]
    protected virtual List<Enum> GetEnumList<T>() where T : Attribute => ReflectionProvider.TryGetAttributeEnumTypes<T>(out var types) ? types.SelectMany(x => Enum.GetValues(x).Cast<Enum>()).ToList() : null;

    protected bool TryGetEnums<TAttribute>(out IEnumerable<Enum> enumerable) where TAttribute : Attribute => (enumerable = GetEnums<TAttribute>()).Equals(Enumerable.Empty<Enum>()) == false;
    protected virtual IEnumerable<Enum> GetEnums<TAttribute>() where TAttribute : Attribute => ReflectionProvider.TryGetAttributeEnumTypes<TAttribute>(out var types) ? types.SelectMany(type => EnumUtil.GetValues(type)) : Enumerable.Empty<Enum>();
    
    public bool TryGetSoundRootObject(Enum type, out GameObject go) => (go = GetSoundRootObject(type)) != null;
    public GameObject GetSoundRootObject(Enum type) => _soundRootDic.TryGetValue(type, out var go) ? go : null;

    public bool TryGetSoundAssetInfoList(Enum type, out List<SoundAssetInfo> list) => (list = GetSoundAssetInfoList(type)) is { Count: > 0 };
    public List<SoundAssetInfo> GetSoundAssetInfoList(Enum type) => soundInfoDic.TryGetValue(type, out var list) ? list : null;
    
    protected virtual void OnAudioConfigurationChanged(bool isChanged) {
        Logger.TraceLog($"Audio Configuration Changed || {isChanged}");
        _onChangeAudioConfigurationCallback?.Invoke(isChanged);
    }
}