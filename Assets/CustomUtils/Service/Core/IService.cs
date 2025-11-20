using System.Threading.Tasks;

public interface IService  {

    protected internal bool IsServing() => false;
    protected internal void Init() { }
    protected internal void Start();
    protected internal void Stop();
    protected internal void Refresh() { }
    protected internal void Remove() { }
}

public interface IAsyncService : IService {
    
    void IService.Init() => _ = InitAsync();
    void IService.Start() => _ = StartAsync();
    void IService.Stop() => _ = StopAsync();
    void IService.Refresh() => _ = RefreshAsync();
    void IService.Remove() => _ = RemoveAsync();

    protected internal async Task InitAsync() => await Task.CompletedTask;
    protected internal Task StartAsync();
    protected internal Task StopAsync();
    protected internal async Task RefreshAsync() => await Task.CompletedTask;
    protected internal async Task RemoveAsync() => await Task.CompletedTask;
}

// TODO. Service 흐름에 맞는 Operation을 우선 개발하여야 함
// public interface IAsyncOperationService : IService {
//     
//     void IService.Init() => _ = InitAsync(new AsyncCustomOperation());
//
//     void IService.Start() => _ = StartAsync(new AsyncCustomOperation());
//     void IService.Stop() => _ = StopAsync(new AsyncCustomOperation());
//     void IService.Refresh() => _ = RefreshAsync(new AsyncCustomOperation());
//     void IService.Remove() => _ = RemoveAsync(new AsyncCustomOperation());
//
//     protected internal async Task InitAsync(AsyncCustomOperation operation) {
//         operation.Done();
//         await Task.CompletedTask;
//     }
//
//     protected internal Task StartAsync(AsyncCustomOperation operation);
//     protected internal Task StopAsync(AsyncCustomOperation operation);
//
//     protected internal async Task RefreshAsync(AsyncCustomOperation operation) {
//         operation.Done();
//         await Task.CompletedTask;
//     }
//
//     protected internal async Task RemoveAsync(AsyncCustomOperation operation) {
//         operation.Done();
//         await Task.CompletedTask;
//     }
// }