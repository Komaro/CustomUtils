using System.Threading;

internal interface ITestHandler {
    
    public void StartTest(CancellationToken token);
}