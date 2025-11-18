using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.SERVICE)]
public class ServiceTestRunner {

    private CancellationTokenSource _tokenSource = new();
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
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