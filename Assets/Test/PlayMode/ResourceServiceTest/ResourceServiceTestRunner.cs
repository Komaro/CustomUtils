using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Profiling.Memory;
using UnityEngine;

[TestFixture]
public class ResourceServiceTestRunner {
    
    [SetUp]
    public void SetUp() {
        if (Service.TryGetService<ResourceService>(out var service)) {
            service.Load(new AssetBundleLoadOrder { assetBundles = new[] { "SFX" } });
        }
    }

    [TearDown]
    public async Task TearDownAsync() {
        if (Service.TryGetService<ResourceService>(out var service)) {
            service.ExecuteOrder(new ResourcesUnloadAllOrder {
                callback = _ => {
                    Logger.TraceLog($"Operation Done");
                    service.Unload(new AssetBundleUnloadOrder { assetBundles = new[] { "SFX" }});
                    MemoryProfiler.TakeSnapshot($"{Constants.Path.MEMORY_CAPTURES_PATH}/TestSnap_02.snap", null);
                }
            });
        }

        await Task.Delay(5000);
    } 
    
    [Test]
    [Performance]
    public void ResourceServiceTest() {
        var group_01 = new SampleGroup("Group_01");
        var group_02 = new SampleGroup("Group_02");
        var group_03 = new SampleGroup("Group_03");
        
        Measure.Method(() => {
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

                var track = service.Get<SoundTrack>("FreeSampleSFX");
                Assert.IsNotNull(track);
                Logger.TraceLog($"Get || {track.name}");
            }
        }).WarmupCount(2).MeasurementCount(5).IterationsPerMeasurement(5).GC().SampleGroup(group_01).Run();
    }

    [Test]
    public void ResourceServiceMemoryProfilerTest() {
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

            var track = service.Get<SoundTrack>("FreeSampleSFX");
            Assert.IsNotNull(track);
            Logger.TraceLog($"Get || {track.name}");
                
            MemoryProfiler.TakeSnapshot($"{Constants.Path.MEMORY_CAPTURES_PATH}/TestSnap_01.snap", null);
        }
    }
}
