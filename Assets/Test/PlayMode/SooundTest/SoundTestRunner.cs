using NUnit.Framework;

[TestFixture]
public class SoundTestRunner {

    [SetUp]
    public void SetUp() => Service.StartService<ResourceService>();

    public void SoundPlayTest() {
        
    }

    [Test]
    public void SoundManagerTest() {
        var soundManager = SampleSoundManager.inst;
        Assert.IsNotNull(soundManager);

        var soundOrder = new SampleSoundOrder(TEST_MASTER_SOUND_TYPE.MASTER_TEST, TEST_CONTROL_SOUND_TYPE.TEST) { name = "Test" };
        Logger.TraceLog($"\n{soundOrder.ToStringAllFields()}");

        soundOrder.type = "Play";
        soundManager.SubmitSoundOrder(soundOrder);

        soundOrder.type = "Stop";
        soundManager.SubmitSoundOrder(soundOrder);

        soundOrder.type = "Mute";
        soundManager.SubmitSoundOrder(soundOrder);
    }
}
