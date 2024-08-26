using System.Threading;

public interface ITestHandler {
    
    public void SetUp() { }
    public void TearDown() { }
    public void StartTest(CancellationToken token);
}