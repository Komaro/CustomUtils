
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[RequiresAttributeImplementation(typeof(MasterSoundAttribute))]
[RequiresAttributeImplementation(typeof(ControlSoundAttribute))]
public abstract class NewSoundBase {
    
    protected SoundCoreBase soundCore;
    protected HashSet<Enum> soundControlSet = new();

    protected Dictionary<Enum, int> maxQueueDic = new();

    public Enum masterEnum;
    public Enum representControlEnum;
    protected bool isMute;

    protected readonly char PATH_SEPARATOR = '/';
    protected readonly char NAME_SEPARATOR = '_';
    protected readonly char EXTENSION_SEPARATOR = '.';

    public NewSoundBase(SoundCoreBase soundCore) {
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

    public abstract void Init();

    public virtual void ExtensionDataRefresh(SoundCoreBase soundCore) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void UpdateQueue();
    protected abstract void UpdateSoundAssetInfo(SoundAssetInfo info);
    
    public abstract void PlayRefresh();
    public abstract void PlayOneShot(SoundTrackEvent trackEvent);
    public abstract void SubmitSoundOrder(NewSoundOrder order);
    
    public virtual void ResetSystemVolume() => SetVolume(1f);
    
    public virtual float GetVolume() => soundCore.GetVolume(masterEnum);
    public abstract float GetVolume(Type controlType);
    
    public virtual void SetVolume(float volume) => soundCore.SetVolume(masterEnum, volume);
    public abstract void SetVolume(Type controlType, float volume);
    
    public virtual bool GetMute() => isMute;
    public abstract void SetMute(bool isMute);
    
    public abstract void Unload(Enum masterType = null, Enum controlType = null);
    public abstract void UnloadAll();

    public bool IsContainsControlType(Enum type) => soundControlSet.Contains(type);
}

public abstract record NewSoundOrder {
    
    public Enum masterType;
    public Enum representControlType;
    
    public NewSoundOrder(Enum masterType, Enum representControlType) {
        this.masterType = masterType;
        this.representControlType = representControlType;
    }
}