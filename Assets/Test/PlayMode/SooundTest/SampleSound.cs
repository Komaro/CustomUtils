using System;

[MasterSound(TEST_MASTER_SOUND_TYPE.MASTER_TEST, TEST_CONTROL_SOUND_TYPE.TEST)]
[ControlSound(TEST_CONTROL_SOUND_TYPE.TEST)]
public class SampleSound : SoundBase {

    private SoundOrder _currentOrder;

    public SampleSound(SoundCoreBase soundCore) : base(soundCore) {
    }

    protected override void UpdateSoundAssetInfo(SoundAssetInfo info) {
    }

    public override void PlayRefresh() {
    }

    public override void SubmitSoundOrder(SoundOrder soundOrder) {
        // Implement SoundOrder Progress
        if (soundOrder is SampleSoundOrder order) {
            Logger.TraceLog($"{order.name} || {order.type}");
            switch (order.type) {
                case "Play":
                    PlaySound(order);
                    break;
                case "Stop":
                    StopSound(order);
                    break;
                case "Mute":
                    MuteSound(order);
                    break;
            }
        }
    }

    private void PlaySound(SampleSoundOrder order) { }
    private void StopSound(SampleSoundOrder order) { }
    private void MuteSound(SampleSoundOrder order) { }
}

public record SampleSoundOrder : SoundOrder {

    public string name;
    public string type;

    public SampleSoundOrder(Enum masterType, Enum representControlType) : base(masterType, representControlType) { }
    
    public SampleSoundOrder(Enum masterType, Enum representControlType, string name, string type) : base(masterType, representControlType) {
        this.name = name;
        this.type = type;
    }
}
