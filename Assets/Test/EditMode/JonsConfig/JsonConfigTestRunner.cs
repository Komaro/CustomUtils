using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VersionControl;
using Task = System.Threading.Tasks.Task;

[Category(TestConstants.Category.FUNCTIONAL)]
public class JsonConfigTestRunner {

    private readonly string TEMP_FOLDER_PATH = Path.Combine(Constants.Path.COMMON_CONFIG_PATH, Constants.Folder.TEMP);

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        SystemUtil.EnsureDirectoryExists(TEMP_FOLDER_PATH);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        // SystemUtil.DeleteDirectory(TEMP_FOLDER_PATH);
    }

    [Test]
    public async Task JsonAutoConfigWithCoroutineTest() {
        var configPath = Path.Combine(TEMP_FOLDER_PATH, "TestConfig".AutoSwitchExtension(Constants.Extension.JSON));
        if (JsonUtil.TryLoadJson<AutoSaveTestConfig>(configPath, out var config) == false) {
            config = new AutoSaveTestConfig();
        }
        
        var configClone = config.Clone<AutoSaveTestConfig>();
        
        config.StartAutoSave(configPath);

        await Task.Delay(500);
        
        var tempIntValue = config.intValue = RandomUtil.GetRandom(0, 10000);
        var tempStringValue = config.stringValue = RandomUtil.GetRandom(20);
        var tempIntArray = config.intArray = RandomUtil.GetRandoms(10).ToArray();

        // TODO. 딥 카피 기능이 아직 구현되지 않아 테스트를 위한 임시 처리 
        var tempIntList = new List<int>(config.intList);
        config.intList = tempIntList;
        config.intList.Add(RandomUtil.GetRandom(0, 10000));
        
        await Task.Delay(7500);

        config.StopAutoSave();
        
        Assert.IsTrue(JsonUtil.TryLoadJson(configPath, out config));
        
        Logger.TraceLog("Check temp value");
        
        Assert.IsTrue(config.intValue == tempIntValue);
        Logger.TraceLog($"{config.intValue} == {tempIntValue}");
        
        Assert.IsTrue(config.stringValue == tempStringValue);
        Logger.TraceLog($"{config.stringValue} == {tempStringValue}");
        
        Assert.IsTrue(config.intArray.Zip(tempIntArray, (a, b) => a == b).All(result => result));
        Logger.TraceLog($"{config.intArray.ToStringCollection(", ")} == {tempIntArray.ToStringCollection(", ")}");
        
        // TODO. 짧은 리스트 쪽으로 최적화되어서 비정상적인 결과 도출
        Assert.IsTrue(config.intList.Zip(tempIntList, (a, b) => a == b).All(result => result));
        Logger.TraceLog($"{config.intList.ToStringCollection(", ")} == {tempIntList.ToStringCollection(", ")}");
        
        Logger.TraceLog("Pass temp value\n");
        
        Logger.TraceLog("Check clone value");
        
        Assert.IsTrue(configClone.intValue != config.intValue);
        Logger.TraceLog($"{configClone.intValue} != {config.intValue}");
        
        Assert.IsTrue(configClone.stringValue != config.stringValue);
        Logger.TraceLog($"{configClone.stringValue} != {config.stringValue}");
        
        Assert.IsTrue(configClone.intArray.Zip(config.intArray, (a, b) => a == b).Any(result => result == false));
        Logger.TraceLog($"{configClone.intArray.ToStringCollection(", ")} != {config.intArray.ToStringCollection(", ")}");
        
        // TODO. 짧은 리스트 쪽으로 최적화되어서 비정상적인 결과 도출
        Assert.IsTrue(configClone.intList.Zip(config.intList, (a, b) => a == b).Any(result => result == false));
        Logger.TraceLog($"{configClone.intList.ToStringCollection(", ")} != {config.intList.ToStringCollection(", ")}");

        Logger.TraceLog("Pass clone value");

        await Task.CompletedTask;
    }

    private class AutoSaveTestConfig : JsonCoroutineAutoConfig {

        public int intValue;
        public string stringValue;
        public int[] intArray;
        public List<int> intList = new();
        
        public override bool IsNull() => this is NullConfig;

        private class NullConfig : AutoSaveTestConfig { }
    }
}
