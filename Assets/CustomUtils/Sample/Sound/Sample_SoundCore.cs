using System.Collections.Generic;
using System.IO;
using UnityEngine.Audio;

public class Sample_SoundCore : SoundCoreBase {

    protected override AudioMixer LoadAudioMixer() => Service.GetService<ResourceService>().Get<AudioMixer>("AudioMixer");
    protected override List<SoundAssetInfo> LoadSoundAssetInfoList() => new();
}
