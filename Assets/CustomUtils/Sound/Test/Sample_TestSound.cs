
using System;

public class Sample_TestSound : SoundBase {

    private SoundOrder _currentOrder;
    
    public Sample_TestSound(SoundCoreBase soundCore) : base(soundCore) { }
    protected override void UpdateSoundAssetInfo(SoundAssetInfo info) { }
    public override void PlayRefresh() { }

    public override void SubmitSoundOrder(SoundOrder soundOrder) {
        // Implement SoundOrder Progress
        if (soundOrder is Sample_SoundOrder order) {
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

    private void PlaySound(Sample_SoundOrder order) { }
    private void StopSound(Sample_SoundOrder order) { }
    private void MuteSound(Sample_SoundOrder order) { }
}

public record Sample_SoundOrder : SoundOrder {

    public string name;
    public string type;
    
    public Sample_SoundOrder(Enum masterType, Enum representControlEnum, string name, string type) : base(masterType, representControlEnum) {
        this.name = name;
        this.type = type;
    }
}