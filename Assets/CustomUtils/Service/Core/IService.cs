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
    
    protected internal Task InitAsync() => Task.CompletedTask;
    protected internal Task StartAsync();
    protected internal Task StopAsync();
    protected internal Task RefreshAsync() => Task.CompletedTask;
    protected internal Task RemoveAsync() => Task.CompletedTask;
}