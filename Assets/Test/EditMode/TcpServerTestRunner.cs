using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture(Author = "KAKA", Category = "Network", Description = "Tcp Server Network Test")]
public class TcpServerTestRunner {
    
    private static SimpleTcpServer _server;
    private static TestTcpClient _client;
    private static CancellationTokenSource _source;

    [SetUp]
    public void SetUpCancellationToken() {
        _source?.Cancel();
        _source = new CancellationTokenSource();
    }
    
    [SetUp]
    public void SetUpSimpleTcpServer() {
        _server?.Stop();
        _server = new SimpleTcpServer(IPAddress.Any, 8890);
    }

    [SetUp]
    public void SetUpTestTcpClient() {
        _client?.Close();
    }

    [TearDown]
    public void TearDownTestRunner() {
        _source?.Cancel();
        _server?.Stop();
        _client?.Close();
    }

    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    [TestCase(typeof(TcpPingServeModule), typeof(TestTcpPingClient))]
    public void TcpServeModuleTest(Type moduleType, Type clientType) {
        try {
            var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
            if (module == null) {
                Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
            }
            
            _server.ChangeServeModule(module);
            _server.Start();

            _client = Activator.CreateInstance(clientType, "localhost", 8890) as TestTcpClient;
            if (_client == null) {
                Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(TestTcpClient)}");
            }
            
            if (_client.Connect() == false) {
                throw new SocketException();
            }
            
            _ = Task.Run(() => _client.StartTest(_source.Token));
            Task.Delay(2500, _source.Token).Wait();
        } catch (SocketException ex) {
            Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    [TestCase(typeof(TcpPingServeModule), typeof(TestTcpPingClient))]
    public async Task TcpServeModuleTestAsync(Type moduleType, Type clientType) {
        try {
            var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
            if (module == null) {
                Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
            }
            
            _server.ChangeServeModule(module);
            _server.Start();

            _client = Activator.CreateInstance(clientType, "localhost", 8890) as TestTcpClient;
            if (_client == null) {
                Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(TestTcpClient)}");
            }
            
            await _client.ConnectAsync();
            _ = Task.Run(() => _client.StartTest(_source.Token));
            await Task.Delay(10000, _source.Token);
        } catch (SocketException ex) {
            Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            await Task.CompletedTask;
        }
    }

    internal interface ITcpTestClient {

        public bool Connect();
        public void Close();
        public Task<bool> ConnectAsync();
        public Task StartTest(CancellationToken token);
    }

    internal abstract class TestTcpClient : ITcpTestClient, IDisposable {

        protected TcpClient client;
        protected string host;
        protected int port;

        private const int CONNECT_TIME_OUT = 2500;

        public bool Connected => client?.Connected ?? false;
        
        public TestTcpClient(string host, int port) {
            client = new TcpClient();
            this.host = host;
            this.port = port;
        }

        ~TestTcpClient() {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Close();

        public virtual bool Connect() {
            var result = client.BeginConnect(host, port, null, client);
            var handler = result.AsyncWaitHandle;
            if (handler.WaitOne(CONNECT_TIME_OUT, false)) {
                if (client.Connected == false || result.IsCompleted == false) {
                    client.Close();
                    return false;
                }

                client.EndConnect(result);
                return true;
            }

            client.Close();
            return false;
        }

        public virtual async Task<bool> ConnectAsync() {
            var connectTask = client.ConnectAsync(host, port);
            await connectTask;
            return connectTask.IsCompletedSuccessfully;
        }

        public virtual void Close() {
            if (client is { Connected: true }) {
                client.Close();
            }
        }

        public abstract Task StartTest(CancellationToken token);
    }
    
    internal class TestTcpPingClient : TestTcpClient {
        
        public TestTcpPingClient(string host, int port) : base(host, port) { }
        
        public override async Task StartTest(CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(32)) {
                while (token.IsCancellationRequested == false) {
                    var buffer = owner.Memory;
                    var length = await client.GetStream().ReadAsync(buffer, token);
                    if (length == 0) {
                        throw new DisconnectSessionException(client);
                    }

                    var textBytes = buffer[..length].ToArray();
                    Logger.TraceLog(textBytes.GetString());

                    var bytes = new byte[sizeof(bool)];
                    var value = true;
                    MemoryMarshal.Write(bytes, ref value);
                    await client.GetStream().WriteAsync(bytes, token);
                }
            }
        }
    }
    
    internal class TestTcpStructClient : TestTcpClient {
        
        public TestTcpStructClient(string host, int port) : base(host, port) { }
        
        public override async Task StartTest(CancellationToken token) {
            if (client.Connected) {
                var session = new TcpSession(client, 9999u);
                var connect = new TcpRequestConnect(session);
                await session.Stream.WriteAsync(connect.ToBytes(), token);
                using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                    var buffer = owner.Memory;
                    var bytesLength = await session.Stream.ReadAsync(buffer, token);
                    if (bytesLength <= 0) {
                        throw new DisconnectSessionException(session);
                    }

                    var header = buffer.ToStruct<TcpHeader>();
                    if (header is { error: TCP_ERROR.NONE }) {
                        _ = Task.Run(() => ReceiveAsync(session, token), token);
                        if (TcpStructHandlerProvider.TryGetSendHandler<TcpRequestTest>(out var handler)) {
                            await handler.SendAsync(session, new TcpRequestTest(15), token);
                        }
                    }
                }
            }
        }

        private async Task ReceiveAsync(TcpSession session, CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(1024)) {
                var buffer = owner.Memory;
                while (session.Connected) {
                     var header = await ReceiveHeaderAsync(session);
                    if (header.sessionId != session.ID) {
                        throw new InvalidDataException();
                    }

                    if (TcpStructHandlerProvider.TryGetReceiveHandler(header.bodyType, out var handler)) {
                        var totalReadLength = 0;
                        using (var memoryStream = new MemoryStream()) {
                            while (totalReadLength < header.byteLength) {
                                var readLength = await session.Stream.ReadAsync(buffer, token);
                                if (readLength <= 0) {
                                    throw new DisconnectSessionException(session);
                                }

                                await memoryStream.WriteAsync(buffer[..readLength], token);
                                totalReadLength += readLength;
                            }

                            await handler.ReceiveAsync(session, memoryStream.ToArray(), token);
                        }
                    }
                }
            }
        }

        private async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session) {
            using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                var buffer = owner.Memory;
                var bytesLength = await session.Stream.ReadAsync(buffer);
                if (bytesLength <= 0) {
                    throw new DisconnectSessionException(session);
                }

                var header = buffer.ToStruct<TcpHeader>();
                if (header.HasValue) {
                    return header.Value;
                }

                throw new InvalidCastException();
            }
        }
    }
}
