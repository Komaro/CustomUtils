using System.Collections.Immutable;
using CustomUtils.Sound.NewSoundSystem;
using NUnit.Framework;
using UnityEngine.Audio;
using UnityEngine.TestTools;

public class NewSoundSystemTestRunner {

    
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        
        
    }

    [OneTimeTearDown]
    public void OnTimeTearDown() {
        
    }
    
    [UnityTest]
    public void BasicNewSoundSystemTest() {
        
    }
}

[MasterSoundEnumType]
public enum SAMPLE_MASTER_SOUND_TYPE {
    NONE,
    SAMPLE_001,
    SAMPLE_002,
}

[ControlSoundEnumType(SAMPLE_MASTER_SOUND_TYPE.SAMPLE_001)]
public enum SAMPLE_CONTROL_SOUND_TYPE {
    NONE,
    SAMPLE_001,
    SAMPLE_002,
}

[MasterSound(SAMPLE_MASTER_SOUND_TYPE.SAMPLE_001, SAMPLE_CONTROL_SOUND_TYPE.SAMPLE_001)]
[ControlSound(SAMPLE_CONTROL_SOUND_TYPE.SAMPLE_001, SAMPLE_CONTROL_SOUND_TYPE.SAMPLE_002)]
public class SampleUnitySound : UnitySound {

    public SampleUnitySound(UnitySoundCore soundCore) : base(soundCore) {
    }

    protected override void UpdateSoundAssetInfo(SoundAssetInfo info) {
        throw new System.NotImplementedException();
    }

    public override void PlayRefresh() {
        throw new System.NotImplementedException();
    }

    public override void SubmitSoundOrder(NewSoundOrder order) {
        throw new System.NotImplementedException();
    }
}

public class SampleUnitySoundCore : UnitySoundCore {

    protected override ImmutableDictionary<SoundType, ImmutableList<SoundAssetInfo>> LoadSoundInfoDic() => throw new System.NotImplementedException();

    public override float GetVolume(SoundType soundTYpe) => throw new System.NotImplementedException();

    public override bool SetVolume(SoundType soundType, float volume) => throw new System.NotImplementedException();

    protected override AudioMixer LoadAudioMixer() => throw new System.NotImplementedException();
}