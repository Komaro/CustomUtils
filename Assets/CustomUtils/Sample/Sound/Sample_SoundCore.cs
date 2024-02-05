using System.Collections.Generic;
using System.IO;
using UnityEngine.Audio;

public class Sample_SoundCore : SoundCoreBase {

    protected override AudioMixer LoadAudioMixer() => ResourceManager.instance.Get<AudioMixer>("AudioMixer");
    protected override List<SoundAssetInfo> LoadSoundAssetInfoList() => new();
}
