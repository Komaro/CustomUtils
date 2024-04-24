using NUnit.Framework;

public class PlayModeTestRunner {

    [Test]
    public void TestResourceService() {
        if (Service.TryGetService<ResourceService>(out var service)) {
            var testPrefab = service.Get("TestPrefab");
            Assert.IsNotNull(testPrefab);
            Logger.TraceLog($"{testPrefab.name}");

            var testPrefab1 = service.Get("TestPrefab_1");
            Assert.IsNotNull(testPrefab1);
            Logger.TraceLog($"{testPrefab1.name}");

            var testPrefab2 = service.Get("TestPrefab_2");
            Assert.IsNotNull(testPrefab2);
            Logger.TraceLog($"{testPrefab2.name}");

            var testPrefab3 = service.Get("TestPrefab_3");
            Assert.IsNotNull(testPrefab3);
            Logger.TraceLog($"{testPrefab3.name}");
        }
        
        Assert.IsNotNull(service);
    }
    
    [Test]
    public void TestSound() {
        Service.StartService<ResourceService>();
        
        var soundManager = TestSoundManager.inst;
        Assert.IsNotNull(soundManager);

        var soundOrder = new Sample_SoundOrder(TEST_MASTER_SOUND_TYPE.MASTER_TEST, TEST_CONTROL_SOUNT_TYPE.TEST) {
            name = "Test"
        };
        
        Logger.TraceLog($"\n{soundOrder.ToStringAllFields()}");

        soundOrder.type = "Play";
        soundManager.SubmitSoundOrder(soundOrder);

        soundOrder.type = "Stop";
        soundManager.SubmitSoundOrder(soundOrder);

        soundOrder.type = "Mute";
        soundManager.SubmitSoundOrder(soundOrder);
    }
}
