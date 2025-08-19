using System.Threading.Tasks;

public interface IService {

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

public interface IOperationService : IService {

    void IService.Init() => _ = InitAsync(null);
    void IService.Start() => _ = StartAsync(null);
    void IService.Stop() => _ = StopAsync(null);
    void IService.Refresh() => _ = RefreshAsync(null);
    void IService.Remove() => _ = RemoveAsync(null);
    
    protected internal ServiceOperation InitOperation() {
        var operation = new ServiceOperation();
        _ = InitAsync(operation);
        return operation;
    }

    protected internal async Task InitAsync(ServiceOperation operation) => await Task.CompletedTask;

    protected internal ServiceOperation StartOperation() {
        var operation = new ServiceOperation();
        _ = StartAsync(operation);
        return operation;
    }
    
    protected internal Task StartAsync(ServiceOperation operation);
    
    protected internal ServiceOperation StopOperation() {
        var operation = new ServiceOperation();
        _ = StopAsync(operation);
        return operation;
    }
    
    protected internal Task StopAsync(ServiceOperation operation);

    protected internal ServiceOperation RefreshOperation() {
        var operation = new ServiceOperation();
        _ = RefreshAsync(operation);
        return operation;
    }
    
    protected internal async Task RefreshAsync(ServiceOperation operation) => await Task.CompletedTask;

    protected internal ServiceOperation RemoveOperation() {
        var operation = new ServiceOperation();
        _ = RemoveAsync(operation);
        return operation;
    }
    
    protected internal async Task RemoveAsync(ServiceOperation operation) => await Task.CompletedTask;
}