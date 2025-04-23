using System.Collections.Generic;
using UnityEngine.Audio;

public class SampleSoundCore : SoundCoreBase {

    protected override AudioMixer LoadAudioMixer() => Service.GetService<ResourceService>().Get<AudioMixer>("AudioMixer");
    protected override List<SoundAssetInfo> LoadSoundAssetInfoList() => new();
}
