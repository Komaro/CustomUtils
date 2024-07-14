public interface IService {

    protected internal bool IsServing() => false;
    protected internal void Init() { }
    protected internal void Start();
    protected internal void Stop();
    protected internal void Refresh() { }
    protected internal void Remove() { }
}