using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

public class ObjectPoolServiceTestRunner {
    
    private const string TEST_PREFAB = "TestPrefab";
    private const string TEST_PREFAB_1 = "TestPrefab_1";
    private const string TEST_PREFAB_2 = "TestPrefab_2";

    [SetUp]
    public void SetUpRemoveService() {
        Service.RemoveService<ResourceService>();
        Service.RemoveService<ObjectPoolService>();
    }
    
    [SetUp]
    public void SetUpStartService() {
        Service.StartService<ResourceService>();
        Service.StartService<ObjectPoolService>();
    }

    [TearDown]
    public void TearDown() {
        
    }

    [Test]
    [Performance]
    public void ObjectPoolServiceTest() {
        var group = new SampleGroup(nameof(ObjectPoolServiceTest));
        Measure.Method(() => {
            var root = new GameObject("TempRoot");
            if (Service.TryGetService<ResourceService>(out var resourceService) && Service.TryGetService<ObjectPoolService>(out var poolService)) {
                var testPrefab = poolService.Get(TEST_PREFAB);
                testPrefab.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab != null);
                Assert.IsTrue(testPrefab.GetInstanceID() != resourceService.Get(TEST_PREFAB).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()));

                var testPrefab_1 = poolService.Get(TEST_PREFAB_1);
                testPrefab_1.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab_1 != null);
                Assert.IsTrue(testPrefab_1.GetInstanceID() != resourceService.Get(TEST_PREFAB_1).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()));
                
                var testPrefab_2 = poolService.Get(TEST_PREFAB_2);
                testPrefab_2.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab_2 != null);
                Assert.IsTrue(testPrefab_2.GetInstanceID() != resourceService.Get(TEST_PREFAB_2).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab);
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab_1);
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab_2);
                Logger.TraceLog(root.transform.GetComponentsInChildren<ObjectPool>().ToStringCollection(x => x.GetName()) + '\n');
            }
            
            Object.Destroy(root);
        }).WarmupCount(1).MeasurementCount(15).IterationsPerMeasurement(1).SampleGroup(group).GC().Run();
    }
}
