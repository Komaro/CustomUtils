using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.SERVICE)]
public class ServiceTestRunner {

    // private CancellationTokenSource _tokenSource = new();
    //
    // [OneTimeSetUp]
    // public void OneTimeSetUp() {
    // }
    //
    // [OneTimeTearDown]
    // public void OneTimeTearDown() {
    //     _tokenSource.Cancel();
    //     if (Service.IsActiveService<OperationTestService>()) {
    //         Service.RemoveService<OperationTestService>();
    //     }
    // }
    //
    // [Test]
    // public async Task ServiceOperationTest() {
    //     var token = _tokenSource.Token;
    //     Assert.IsNotNull(await Service.StartServiceAsync<OperationTestService>());
    //     Service.GetService<OperationTestService>();
    //     
    //     
    //     while (operation.IsDone == false) {
    //         token.ThrowIfCancellationRequested();
    //         await Task.Delay(250, token);
    //     }
    //     
    //     Assert.IsTrue(Service.TryGetService<OperationTestService>(out _));
    //     Assert.IsTrue(Service.RemoveService<OperationTestService>());
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
//             await Task.Delay(500);
//         }
//     }
//
//     async Task IAsyncService.StartAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"StartAsync || {i} || {(i / 10f).ToPercent()}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(500);
//         }
//     }
//
//     async Task IAsyncService.StopAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"StopAsync || {i} || {(i / 10f).ToPercent()}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(500);
//         }
//     }
//
//     async Task IAsyncService.RefreshAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"RefreshAsync || {i} || {(i / 10f).ToPercent(1)}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(500);
//         }
//     }
//
//     async Task IAsyncService.RemoveAsync(ServiceOperation operation) {
//         operation.Init();
//         await Task.Yield();
//         for (var i = 1; i <= MAX_COUNT; i++) {
//             Logger.TraceLog($"RemoveAsync || {i} || {(i / 10f).ToPercent(1)}%");
//             operation.Report(i, MAX_COUNT);
//             await Task.Delay(500);
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