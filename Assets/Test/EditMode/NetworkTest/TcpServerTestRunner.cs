using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class TcpServerTestRunner {
    
    public static SimpleTcpServer server;
    private static ITcpClient _tcpClient;
    private static CancellationTokenSource tokenSource;

    [SetUp]
    public void SetUp() {
        LogAssert.ignoreFailingMessages = true;
    }
    
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
        server?.Dispose();
        server = new SimpleTcpServer(IPAddress.Any, 8890);
    }
    
    [TearDown]
    public void TearDownTestRunner() {
        tokenSource?.Cancel();
        server?.Stop();
        _tcpClient?.Close();
    }

    [TearDown]
    public void TearDown() {
        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void Clear() {
        tokenSource?.Cancel();
        _tcpClient?.Close();
        server?.Stop();
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

    [TestCase(typeof(TcpJsonServeModule), typeof(TestTcpJsonClient))]
    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    public async Task TcpClientTestAsync(Type moduleType, Type clientType) {
        try {
            var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
            if (module == null) {
                Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
            }

            server.ChangeServeModule(module);
            server.Start();
            
            await Task.Delay(1000, tokenSource.Token);

            _tcpClient = Activator.CreateInstance(clientType, "localhost", 8890) as ITcpClient;
            if (_tcpClient == null) {
                Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(ITcpClient)}");
            }

            await _tcpClient.Start();
            if (_tcpClient is ITestHandler testHandler) {
                await Task.Run(() => testHandler.StartTest(tokenSource.Token), tokenSource.Token);
            }

            await Task.Delay(6000, tokenSource.Token);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            await Task.CompletedTask;
        }
    }
}