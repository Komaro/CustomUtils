using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CustomUtils.Sound.NewSoundSystem {
    
    [RequiresAttributeImplementation(typeof(MasterSoundAttribute))]
    [RequiresAttributeImplementation(typeof(ControlSoundAttribute))]
    [TestRequired]
    public abstract class SoundBase<TSoundCore> where TSoundCore : SoundCoreBase {
    
        protected readonly TSoundCore soundCore;

        protected ImmutableHashSet<SoundType> soundTypeSet;
    
        public MasterType masterType;
        public ControlType representControlType;
    
        protected bool isMute;

        protected const char PATH_SEPARATOR = '/';
        protected const char NAME_SEPARATOR = '_';
        protected const char EXTENSION_SEPARATOR = '.';

        public SoundBase(TSoundCore soundCore) {
            this.soundCore = soundCore.ThrowIfUnexpectedNull(nameof(this.soundCore));

            var masterAttribute = GetType().GetCustomAttribute<MasterSoundAttribute>().ThrowIfUnexpectedNull();
            masterType = soundCore.GetSoundType(masterAttribute.masterEnum) as MasterType;
            masterType.ThrowIfUnexpectedNull(nameof(masterType));
            
            representControlType = soundCore.GetSoundType(masterAttribute.representControlEnum) as ControlType;
            representControlType.ThrowIfUnexpectedNull(nameof(representControlType));
        
            var controlAttribute = GetType().GetCustomAttribute<ControlSoundAttribute>().ThrowIfUnexpectedNull();
            soundTypeSet = controlAttribute.controlList.WhereSelect(soundCore.IsValidSoundType, soundCore.GetSoundType).Append(masterType).Append(representControlType).ToImmutableHashSetWithDistinct();
        }

        public virtual void Init() {
        
        }
    
        public virtual void ExtensionDataRefresh(SoundCoreBase soundCore) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void UpdateQueue();
        protected abstract void UpdateSoundAssetInfo(SoundAssetInfo info);
    
        public abstract void PlayRefresh();
        public abstract void SubmitSoundOrder(NewSoundOrder order);
    
        public virtual void ResetSystemVolume() => SetVolume(1f);
    
        public float GetVolume() => soundCore.GetVolume(masterType);
        public float GetVolume(Enum enumValue) => soundCore.GetVolume(enumValue);
        public float GetVolume(SoundType soundType) => soundCore.GetVolume(soundType);
    
        public void SetVolume(float volume) => soundCore.SetVolume(masterType, volume);
        public void SetVolume(Enum enumValue, float volume) => soundCore.SetVolume(enumValue, volume);
        public void SetVolume(SoundType soundType, float volume) => soundCore.SetVolume(soundType, volume);

        public virtual bool GetMute() => isMute;
        public abstract void SetMute(bool isMute);

        public void Unload(Enum enumValue) {
            if (TryGetSoundType(enumValue, out var soundType) && soundTypeSet.Contains(soundType)) {
                if (soundType == masterType) {
                    UnloadAll();
                } else {
                    Unload(soundType);
                }
            }
        }

        public abstract void Unload(SoundType soundType);
        public abstract void UnloadAll();

        public bool IsContainsSoundType(Enum type) => TryGetSoundType(type, out var soundType) && soundTypeSet.Contains(soundType);

        protected bool TryGetSoundType(Enum enumValue, out SoundType soundType) => (soundType = GetSoundType(enumValue)) != null;
        protected SoundType GetSoundType(Enum enumValue) => soundCore.GetSoundType(enumValue);
    }

    public abstract record NewSoundOrder {
    
        public Enum masterType;
        public Enum representControlType;
    
        public NewSoundOrder(Enum masterType, Enum representControlType) {
            this.masterType = masterType;
            this.representControlType = representControlType;
        }
    }
}