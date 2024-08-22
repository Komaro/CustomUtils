using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;

public class TcpServerTestRunner {
    
    private static SimpleTcpServer _server;
    private static ITcpClient _tcpClient;
    private static CancellationTokenSource tokenSource;

    [SetUp]
    public void SetUpCancellationToken() {
        tokenSource?.Cancel();
        tokenSource = new CancellationTokenSource();
    }
    
    [SetUp]
    public void SetUpTestTcpClient() {
        _tcpClient?.Close();
    }
    
    [SetUp]
    public void SetUpSimpleTcpServer() {
        _server?.Dispose();
        _server = new SimpleTcpServer(IPAddress.Any, 8890);
    }
    
    [TearDown]
    public void TearDownTestRunner() {
        tokenSource?.Cancel();
        _server?.Stop();
        _tcpClient?.Close();
    }

    [Test]
    public void Clear() {
        tokenSource?.Cancel();
        _tcpClient?.Close();
        _server?.Stop();
    }
    
    [Test]
    public void Test() {
        var singleton = TcpJsonHandlerProvider.instance;
        if (singleton != null) {
            Logger.TraceLog(singleton.GetType().Name);
        }
    }

    [Test]
    [Performance]
    public void TcpJsonHandlerPerformanceTest() {
        var jsonHandlerGroup = new SampleGroup("JsonHandler");
        Measure.Method(() => {
                
            })
            .MeasurementCount(25)
            .IterationsPerMeasurement(1)
            .SampleGroup(jsonHandlerGroup)
            .Run();
    }

    [Test]
    public void TempTest() {
        var list = ReflectionProvider.GetInterfaceTypes<ITcpHandler>().ToList();
        Logger.TraceLog("\n" + list.ToStringCollection('\n'));
    }

    [TestCase(typeof(TcpJsonServeModule), typeof(TestTcpJsonClient))]
    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructWrapperClient))]
    public async Task TcpClientTestAsync(Type moduleType, Type clientType) {
        try {
            var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
            if (module == null) {
                Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
            }

            _server.ChangeServeModule(module);
            _server.Start();
            
            await Task.Delay(1000, tokenSource.Token);

            _tcpClient = Activator.CreateInstance(clientType, "localhost", 8890) as ITcpClient;
            if (_tcpClient == null) {
                Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(ITcpClient)}");
            }

            await _tcpClient.Start();
            if (_tcpClient is ITestHandler testHandler) {
                _ = Task.Run(() => testHandler.StartTest(tokenSource.Token), tokenSource.Token);
            }

            await Task.Delay(5000, tokenSource.Token);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            await Task.CompletedTask;
        }
    }
}