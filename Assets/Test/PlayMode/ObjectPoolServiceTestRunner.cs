using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

public class ObjectPoolServiceTestRunner {
    
    private const string TEST_PREFAB = "TestPrefab";
    private const string TEST_PREFAB_1 = "TestPrefab_1";
    private const string TEST_PREFAB_2 = "TestPrefab_2";

    [OneTimeSetUp]
    public void SetUpStartService() {
        Service.StartService<ResourceService>();
        Service.StartService<ObjectPoolService>();
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

    [TestCase(10)]
    [TestCase(20)]
    public void ObjectPoolServiceRepeatTest(int repeatCount) {
        if (Service.TryGetService<ObjectPoolService>(out var poolService)) {
            var stack = new Stack<GameObject>();
            for (var count = 0; count < repeatCount; count++) {
                stack.Push(poolService.Get(TEST_PREFAB));
                Logger.TraceLog(stack.ToStringCollection(x => x.name, ", "));
            }

            while (stack.TryPop(out var go)) {
                poolService.Release(go);
                Logger.TraceLog(stack.ToStringCollection(x => x.name, ", "));
            }
        }
    }

    [TestCase(60)]
    public void ObjectPoolServiceMaximumTest(int repeatCount) {
        if (Service.TryGetService<ObjectPoolService>(out var poolService)) {
            var stack = new Stack<GameObject>();
            for (var count = 0; count < repeatCount; count++) {
                var go = poolService.Get(TEST_PREFAB);
                if (go != null) {
                    stack.Push(go);
                }
                
                Logger.TraceLog($"{poolService.GetCountAll(TEST_PREFAB)} || {poolService.GetCountActive(TEST_PREFAB)} || {poolService.GetCountInactive(TEST_PREFAB)}");
            }
            
            Assert.IsTrue(poolService.GetCountAll(TEST_PREFAB) == repeatCount);
            Logger.TraceLog("Success Get Test\n");
            
            while (stack.TryPop(out var go)) {
                poolService.Release(go);
                Logger.TraceLog($"{poolService.GetCountAll(TEST_PREFAB)} || {poolService.GetCountActive(TEST_PREFAB)} || {poolService.GetCountInactive(TEST_PREFAB)}");
            }
            
            Assert.IsTrue(poolService.GetCountInactive(TEST_PREFAB) != repeatCount);
            Assert.IsTrue(poolService.GetCountAll(TEST_PREFAB) == poolService.GetCountInactive(TEST_PREFAB));
        }
    }
}
