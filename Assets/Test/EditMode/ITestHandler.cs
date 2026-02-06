using System.Threading;
using System.Threading.Tasks;

public interface ITestHandler {
    
    public void SetUp() { }
    public void TearDown() { }
    public void StartTest(CancellationToken token);
}

public interface IAsyncTestHandler {

    public Task SetUpAsync(CancellationToken token);
    public Task TearDownAsync(CancellationToken token);
    public Task StartTestAsync(CancellationToken token);
}