using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.SERVICE)]
[Category(TestConstants.Category.PERFORMANCE)]
public class ServiceTestRunner {

    private CancellationTokenSource _tokenSource = new();
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
    }
    
    [Test]
    public async Task TimeCacheServiceTest() {
        Assert.IsTrue(Service.TryGetService<TimeCacheService>(out var service));

        Assert.IsTrue(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.TIME, 4));
        await Task.Delay(5000);
        Assert.IsTrue(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.TIME, 4));
        Logger.TraceLog($"Pass || {DateTime.Now}");
        
        Assert.IsTrue(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.CACHE, 3));
        await Task.Delay(1000);
        Assert.IsFalse(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.CACHE, 3));
        await Task.Delay(2500);
        Assert.IsTrue(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.CACHE, 3));
        Logger.TraceLog($"Pass || {DateTime.Now}");
        
        Assert.IsFalse(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.TIME, 4));
        await Task.Delay(1500);
        Assert.IsTrue(service.CheckAndUpdateElapsed(TIME_CACHE_TEST.TIME, 4));
        Logger.TraceLog($"Pass || {DateTime.Now}");
        
        Logger.TraceLog($"Test all pass");
    }

    [Performance]
    [TestCase(10000)]
    [TestCase(50000)]
    [TestCase(100000)]
    [TestCase(500000)]
    public void TimeCacheServicePerformanceTest(int count) {
        Assert.IsTrue(Service.TryGetService<TimeCacheService>(out var service));

        var before = new SampleGroup("Before Allocate Memory", SampleUnit.Megabyte);
        var group = new SampleGroup("TimeCache GC");
        var after = new SampleGroup("After Allocate Memory", SampleUnit.Megabyte);
        
        Measure.Custom(before, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
        Measure.Method(() => _ = service.CheckAndUpdateElapsed(TIME_CACHE_TEST.TIME, 4)).SampleGroup(group).WarmupCount(1).MeasurementCount(20).IterationsPerMeasurement(count).GC().Run();
        Measure.Custom(after, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
    }

    public enum TIME_CACHE_TEST {
        TIME,
        CACHE,
        TEST
    }
    
    // [OneTimeTearDown]
    // public void OneTimeTearDown() {
    //     _tokenSource.Cancel();
    //     if (ServiceEx.IsActiveService<OperationTestService>()) {
    //         ServiceEx.Remove<OperationTestService>();
    //     }
    // }
    //
    // [Test]
    // public async Task ServiceExAsyncOperationTest() {
    //     var token = _tokenSource.Token;
    //     
    //     var operation = ServiceEx.StartAsyncOperation(typeof(OperationTestService));
    //     while (operation.IsDone == false) {
    //         token.ThrowIfCancellationRequested();
    //         await Task.Delay(50, token);
    //     }
    //
    //     var service = await ServiceEx.GetAsync<OperationTestService>();
    //     Assert.IsNotNull(service);
    //     operation = ServiceEx.StopAsyncOperation(typeof(OperationTestService));
    //
    //     while (operation.IsDone == false) {
    //         token.ThrowIfCancellationRequested();
    //         await Task.Delay(50, token);
    //     }
    // }
}

// public class OperationTestService : IAsyncService {
//
//     private const int MAX_COUNT = 10;
//
//     Task IAsyncService.StartAsync() {
//         return Task.CompletedTask;
//     }
//
//     Task IAsyncService.StopAsync() {
//         return Task.CompletedTask;
//     }
//
//     async Task IAsyncService.InitAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"InitAsync || {i} || {(i / 10f).ToPercent()}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(200);
//         }
//     }
//
//     async Task IAsyncService.StartAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"StartAsync || {i} || {(i / 10f).ToPercent()}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(200);
//         }
//     }
//
//     async Task IAsyncService.StopAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"StopAsync || {i} || {(i / 10f).ToPercent()}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(200);
//         }
//     }
//
//     async Task IAsyncService.RefreshAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"RefreshAsync || {i} || {(i / 10f).ToPercent(1)}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(200);
//         }
//     }
//
//     async Task IAsyncService.RemoveAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"RemoveAsync || {i} || {(i / 10f).ToPercent(1)}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(200);
//         }
//     }
// }

public enum FIRST {
    NONE,
    FIRST,
}

public enum SECOND {
    NONE,
    SECOND,
}