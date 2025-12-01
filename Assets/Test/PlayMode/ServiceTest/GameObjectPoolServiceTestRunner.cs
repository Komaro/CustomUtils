using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

[Category(TestConstants.Category.SERVICE)]
public class GameObjectPoolServiceTestRunner {
    
    private const string TEST_PREFAB = "TestPrefab";
    private const string TEST_PREFAB_1 = "TestPrefab_1";
    private const string TEST_PREFAB_2 = "TestPrefab_2";

    [OneTimeSetUp]
    public void SetUpStartService() {
        Service.StartService<ResourceService>();
        Service.StartService<GameObjectPoolService>();
    }

    [SetUp]
    public void SetUp() {
        if (Service.TryGetService<GameObjectPoolService>(out var service)) {
            service.Clear(TEST_PREFAB);
            service.Clear(TEST_PREFAB_1);
            service.Clear(TEST_PREFAB_2);
        }
    }

    [Test]
    [Performance]
    public void ObjectPoolServicePerformanceTest() {
        var group = new SampleGroup(nameof(ObjectPoolServicePerformanceTest));
        Measure.Method(() => {
            var root = new GameObject("TempRoot");
            if (Service.TryGetService<ResourceService>(out var resourceService) && Service.TryGetService<GameObjectPoolService>(out var poolService)) {
                var testPrefab = poolService.Get(TEST_PREFAB);
                testPrefab.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab != null);
                Assert.IsTrue(testPrefab.GetInstanceID() != resourceService.Get(TEST_PREFAB).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()));

                var testPrefab_1 = poolService.Get(TEST_PREFAB_1);
                testPrefab_1.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab_1 != null);
                Assert.IsTrue(testPrefab_1.GetInstanceID() != resourceService.Get(TEST_PREFAB_1).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()));
                
                var testPrefab_2 = poolService.Get(TEST_PREFAB_2);
                testPrefab_2.transform.SetParent(root.transform);
                Assert.IsTrue(testPrefab_2 != null);
                Assert.IsTrue(testPrefab_2.GetInstanceID() != resourceService.Get(TEST_PREFAB_2).GetInstanceID());
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab);
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab_1);
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()));
                
                poolService.Release(testPrefab_2);
                Logger.TraceLog(root.transform.GetComponentsInChildren<GameObjectPool>().ToStringCollection(x => x.GetName()) + '\n');
            }
            
            Object.Destroy(root);
            Logger.TraceLog("Pass Performance Test");
        }).WarmupCount(1).MeasurementCount(15).IterationsPerMeasurement(1).SampleGroup(group).GC().Run();
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(30)]
    [TestCase(40)]
    public void ObjectPoolServiceRepeatTest(int repeatCount) {
        if (Service.TryGetService<GameObjectPoolService>(out var service)) {
            var stack = new Stack<GameObject>();
            for (var count = 0; count < repeatCount; count++) {
                stack.Push(service.Get(TEST_PREFAB));
                Logger.TraceLog(stack.ToStringCollection(x => x.name, ", "));
            }
            
            Assert.IsTrue(service.GetCountActive(TEST_PREFAB) == repeatCount);
            Assert.IsTrue(service.GetCountActive(TEST_PREFAB) == stack.Count);
            Logger.TraceLog("Pass Get Test");

            while (stack.TryPop(out var go)) {
                service.Release(go);
                Logger.TraceLog(stack.ToStringCollection(x => x.name, ", "));
            }
            
            Assert.IsTrue(service.GetCountActive(TEST_PREFAB) == 0);
            Assert.IsTrue(service.GetCountInactive(TEST_PREFAB) == repeatCount);
            Logger.TraceLog("Pass Release Test");
        }
    }

    [TestCase(60)]
    [TestCase(120)]
    [TestCase(180)]
    public void ObjectPoolServiceMaximumTest(int repeatCount) {
        if (Service.TryGetService<GameObjectPoolService>(out var service)) {
            var stack = new Stack<GameObject>();
            for (var count = 0; count < repeatCount; count++) {
                var go = service.Get(TEST_PREFAB);
                if (go != null) {
                    stack.Push(go);
                }
                
                Logger.TraceLog($"{service.GetCountAll(TEST_PREFAB)} || {service.GetCountActive(TEST_PREFAB)} || {service.GetCountInactive(TEST_PREFAB)}");
            }
            
            Assert.IsTrue(service.GetCountAll(TEST_PREFAB) == repeatCount);
            Logger.TraceLog("Pass Get Test\n");
            
            while (stack.TryPop(out var go)) {
                service.Release(go);
                Logger.TraceLog($"{service.GetCountAll(TEST_PREFAB)} || {service.GetCountActive(TEST_PREFAB)} || {service.GetCountInactive(TEST_PREFAB)}");
            }
            
            Assert.IsTrue(service.GetCountInactive(TEST_PREFAB) != repeatCount);
            Assert.IsTrue(service.GetCountAll(TEST_PREFAB) == service.GetCountInactive(TEST_PREFAB));
        }
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(30)]
    [TestCase(40)]
    [TestCase(50)]
    public void ObjectPoolServicePreloadTest(int preloadCount) {
        if (Service.TryGetService<GameObjectPoolService>(out var service)) {
            service.Preload(TEST_PREFAB, preloadCount);
            Logger.TraceLog($"{service.GetCountAll(TEST_PREFAB)} || {service.GetCountActive(TEST_PREFAB)} || {service.GetCountInactive(TEST_PREFAB)}");
            Assert.IsTrue(service.GetCountAll(TEST_PREFAB) == preloadCount);
            Assert.IsTrue(service.GetCountActive(TEST_PREFAB) == 0);
            Assert.IsTrue(service.GetCountInactive(TEST_PREFAB) == preloadCount);
            Logger.TraceLog("Pass Preload Test");
            
            service.Clear(TEST_PREFAB);
            Logger.TraceLog($"{service.GetCountAll(TEST_PREFAB)} || {service.GetCountActive(TEST_PREFAB)} || {service.GetCountInactive(TEST_PREFAB)}");
            Assert.IsTrue(service.GetCountAll(TEST_PREFAB) == 0);
            Assert.IsTrue(service.GetCountActive(TEST_PREFAB) == 0);
            Assert.IsTrue(service.GetCountInactive(TEST_PREFAB) == 0);
            Logger.TraceLog("Pass Clear Test");
        }
    }
}
