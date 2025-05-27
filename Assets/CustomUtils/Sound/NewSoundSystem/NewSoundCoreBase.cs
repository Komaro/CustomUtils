using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

namespace CustomUtils.Sound.NewSoundSystem {
    
    [TestRequired]
    public abstract class SoundCoreBase : MonoBehaviour, IDisposable {

        protected ImmutableDictionary<Enum, SoundType> soundTypeDic = ImmutableDictionary<Enum, SoundType>.Empty;
        protected ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>> controlTypeDic = ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>>.Empty;
        protected ImmutableDictionary<SoundType, ImmutableList<SoundAssetInfo>> assetInfoDic = ImmutableDictionary<SoundType, ImmutableList<SoundAssetInfo>>.Empty;
        protected readonly ConcurrentDictionary<SoundType, GameObject> soundObjectDic = new();

        protected bool _isInit;

        protected static Action<bool> _onChangeAudioConfiguration;

        public static global::SoundCoreBase Create<T>(Action<bool> onChangeAudioConfiguration = null) where T : global::SoundCoreBase {
            _onChangeAudioConfiguration = onChangeAudioConfiguration;
            var go = new GameObject(typeof(T).Name);
            return go.AddComponent<T>();
        }

        private void Awake() {
            DontDestroyOnLoad(gameObject);

            soundTypeDic = LoadSoundTypeDic();
            controlTypeDic = LoadControlTypeDic();
            assetInfoDic = LoadSoundInfoDic();

            AttachSoundObject();
        
            OnAwake();
        
            _isInit = true;
        }

        protected abstract void OnAwake();

        private void OnDestroy() {
            Dispose();
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        public abstract void Dispose();

        [RefactoringRequired]
        [TestRequired]
        protected virtual ImmutableDictionary<Enum, SoundType> LoadSoundTypeDic() => GetEnums<MasterSoundEnumTypeAttribute>().ToDictionary(enumValue => enumValue, enumValue => new MasterType(enumValue) as SoundType).Pipe(dictionary => dictionary.Concat(ReflectionProvider.GetAttributeEnumInfos<ControlSoundEnumTypeAttribute>()
            .WhereSelectMany(info => dictionary.ContainsKey(info.attribute.masterType), info => EnumUtil.GetValues(info.attribute.masterType).Select(enumValue => new KeyValuePair<Enum, SoundType>(enumValue, new ControlType(dictionary[enumValue], enumValue))))).ToImmutableDictionary());

        [TestRequired]
        protected virtual ImmutableDictionary<MasterType, ImmutableHashSet<ControlType>> LoadControlTypeDic() => soundTypeDic.Values.OfType<ControlType>().GroupBy(controlType => controlType.MasterType as MasterType).ToImmutableDictionary(grouping => grouping.ToImmutableHashSet());

        [TestRequired]
        protected abstract ImmutableDictionary<SoundType, ImmutableList<SoundAssetInfo>> LoadSoundInfoDic();

        public virtual bool IsInitialize() => _isInit;
    
        public abstract float GetVolume(Enum type);
        public abstract float GetVolume<T>(T type) where T : struct, Enum;
        public abstract float GetVolume(SoundType soundTYpe);
    
        public abstract bool SetVolume(Enum type, float volume);
        public abstract bool SetVolume<T>(T type, float volume) where T : struct, Enum;
        public abstract bool SetVolume(SoundType soundType, float volume);
    
        [TestRequired]
        private void AttachSoundObject() {
            foreach (var soundType in soundTypeDic.Values) {
                AttachSoundObject(soundType);
            }
        }

        protected virtual GameObject AttachSoundObject(SoundType type) {
            if (soundObjectDic.ContainsKey(type)) {
                Logger.TraceLog($"{type.Value} is already type", Color.yellow);
                return null;
            }

            GameObject go = null;
            switch (type) {
                case MasterType masterType:
                    go = CreateObject(masterType, transform);
                    break;
                case ControlType controlType:
                    if (soundObjectDic.TryGetValue(type, out var parent) == false) { 
                        parent = AttachSoundObject(controlType.MasterType);
                        if (parent == null) {
                            throw new NullReferenceException<GameObject>(nameof(parent));
                        }
                    }

                    go = CreateObject(controlType, parent.transform);
                    break;
            }

            go.ThrowIfNull(nameof(go));
        
            soundObjectDic.TryAdd(type, go);
            return go;
        }

        [TestRequired]
        protected virtual GameObject CreateObject(SoundType type, Transform parent) {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(parent);
            return go;
        }

        protected bool TryGetEnums<TAttribute>(out IEnumerable<Enum> enumerable) where TAttribute : Attribute => (enumerable = GetEnums<TAttribute>()).Equals(Enumerable.Empty<Enum>()) == false;
        protected virtual IEnumerable<Enum> GetEnums<TAttribute>() where TAttribute : Attribute => ReflectionProvider.TryGetAttributeEnumTypes<TAttribute>(out var types) ? types.SelectMany(type => EnumUtil.GetValues(type)) : Enumerable.Empty<Enum>();
    
        public bool TryGetSoundObject(SoundType type, out GameObject go) => (go = GetSoundObject(type)) != null;
        public GameObject GetSoundObject(SoundType type) => soundObjectDic.TryGetValue(type, out var go) ? go : null;

        public bool TryGetAssetInfoList(Enum enumValue, out ImmutableList<SoundAssetInfo> list) => (list = GetAssetInfoList(enumValue)).Equals(ImmutableList<SoundAssetInfo>.Empty) == false;
        public ImmutableList<SoundAssetInfo> GetAssetInfoList(Enum enumValue) => TryGetSoundType(enumValue, out var soundType) && soundType is MasterType masterType ? GetAssetInfoList(masterType) : ImmutableList<SoundAssetInfo>.Empty;

        public bool TryGetAssetInfoList(SoundType masterType, out ImmutableList<SoundAssetInfo> list) => (list = GetAssetInfoList(masterType)).Equals(ImmutableList<SoundAssetInfo>.Empty) == false;
        public ImmutableList<SoundAssetInfo> GetAssetInfoList(SoundType soundType) => assetInfoDic.TryGetValue(soundType, out var list) ? list : ImmutableList<SoundAssetInfo>.Empty;

        public bool TryGetSoundType(Enum enumValue, out SoundType soundType) => (soundType = GetSoundType(enumValue)) != null;
        public SoundType GetSoundType(Enum enumValue) => soundTypeDic.TryGetValue(enumValue, out var soundType) ? soundType : null;

        public bool IsValidSoundType(Enum enumValue) => TryGetSoundType(enumValue, out var soundType) && IsValidSoundType(soundType);
        public bool IsValidSoundType(SoundType type) => soundObjectDic.ContainsKey(type);

        protected virtual void OnAudioConfigurationChanged(bool isChanged) {
            Logger.TraceLog($"Audio Configuration Changed || {isChanged}");
            _onChangeAudioConfiguration?.Invoke(isChanged);
        }
    }
    
    public abstract record SoundType(Enum Value) {
    
        public Enum Value { get; } = Value;
    
        public string Name => Value?.ToString() ?? string.Intern("Null");
    
        public static implicit operator Enum(SoundType soundType) => soundType.Value;
        public override string ToString() => Value.ToString();
    }

    public record MasterType(Enum Value) : SoundType(Value) {

        public static implicit operator MasterType(Enum enumValue) => new(enumValue);
    }

    public record ControlType(SoundType MasterType, Enum Value) : SoundType(Value) {

        public SoundType MasterType { get; } = MasterType;
    }
}