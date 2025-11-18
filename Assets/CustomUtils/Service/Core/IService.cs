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
    
    // protected internal async Task InitAsync(ServiceOperation operation) => await Task.CompletedTask;
    // protected internal Task StartAsync(ServiceOperation operation);
    // protected internal Task StopAsync(ServiceOperation operation);
    // protected internal async Task RefreshAsync(ServiceOperation operation) => await Task.CompletedTask;
    // protected internal async Task RemoveAsync(ServiceOperation operation) => await Task.CompletedTask;
}