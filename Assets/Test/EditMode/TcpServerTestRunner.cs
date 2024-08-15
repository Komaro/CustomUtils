using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

[TestFixture(Author = "KAKA", Category = "Network", Description = "Tcp Server Network Test")]
public class TcpServerTestRunner {
    
    private static SimpleTcpServer _server;
    private static SimpleTcpClient _client;
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

    // [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    // [TestCase(typeof(TcpPingServeModule), typeof(TestTcpPingClient))]
    // [TestCase(typeof(TcpJsonServeModule), typeof(TestTcpJsonClient))]
    // public void TcpServeModuleTest(Type moduleType, Type clientType) {
    //     try {
    //         var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
    //         if (module == null) {
    //             Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
    //         }
    //         
    //         _server.ChangeServeModule(module);
    //         _server.Start();
    //
    //         _client = Activator.CreateInstance(clientType, "localhost", 8890) as SimpleTcpClient;
    //         if (_client == null) {
    //             Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(SimpleTcpClient)}");
    //         }
    //         
    //         if (_client.Connect() == false) {
    //             throw new SocketException();
    //         }
    //         
    //         _ = Task.Run(() => _client.StartTest(_source.Token));
    //         Task.Delay(2500, _source.Token).Wait();
    //     } catch (SocketException ex) {
    //         Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
    //     } catch (Exception ex) {
    //         Logger.TraceError(ex);
    //     }
    // }

    [TestCase(typeof(TcpStructServeModule), typeof(TestTcpStructClient))]
    [TestCase(typeof(TcpPingServeModule), typeof(TestTcpPingClient))]
    [TestCase(typeof(TcpJsonServeModule), typeof(TestTcpJsonClient))]
    public async Task TcpServeModuleTestAsync(Type moduleType, Type clientType) {
        try {
            var module = Activator.CreateInstance(moduleType) as ITcpServeModule;
            if (module == null) {
                Assert.Fail($"The {moduleType.Name} class does not implement the {nameof(ITcpServeModule)} interface");
            }
            
            _server.ChangeServeModule(module);
            _server.Start();

            _client = Activator.CreateInstance(clientType, "localhost", 8890) as SimpleTcpClient;
            if (_client == null) {
                Assert.Fail($"{nameof(clientType.Name)} does not inherit from {nameof(SimpleTcpClient)}");
            }

            if (await _client.ConnectAsync()) {
                _ = Task.Run(() => _client.StartTest(_source.Token));
            } else {
                throw new DisconnectSessionException(_client.Client);
            }
            await Task.Delay(10000, _source.Token);
        } catch (SocketException ex) {
            Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            await Task.CompletedTask;
        }
    }
    
    // TODO. 모듈화 수정
    public interface ITcpClient<THeader, in TData> : ITcpReceiveModule<THeader>, ITcpSendModule<TData> {
        
        public Task Run();
        public Task<bool> ConnectAsync(TcpClient client);
    }

    public abstract class TestSimpleTcpClient<THeader, TData> : ITcpClient<THeader, TData> where THeader : ITcpPacket where TData : ITcpPacket {

        protected TcpSession session;
        protected string host;
        protected int port;

        protected Channel<TData> _sendChannel = Channel.CreateBounded<TData>(new BoundedChannelOptions(5));

        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        
        public TestSimpleTcpClient(string host, int port) {
            this.host = host;
            this.port = port;
        }

        public virtual async Task Run() {
            if (session != null && session.IsValid() && session.Connected) {
                session.Close();
            }

            var client = new TcpClient();
            if (await ConnectAsync(client)) {
                session = await ConnectSessionAsync(client, _source.Token);
                if (session.IsValid()) {
                    _ = Task.Run(() => ReceiveAsync(session, _source.Token));
                    _ = Task.Run(() => ReadSendChannelAsync(_source.Token));
                }
            } else {
                throw new DisconnectSessionException(session);
            }
        }

        public async Task<bool> ConnectAsync(TcpClient client) {
            var connectTask = client.ConnectAsync(host, port);
            await connectTask;
            return connectTask.IsCompletedSuccessfully;
        }
        
        public abstract Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
        
        protected async Task ReceiveAsync(TcpSession session, CancellationToken token) {
            while (session.Connected && token.IsCancellationRequested == false) {
                token.ThrowIfCancellationRequested();
                var header = await ReceiveHeaderAsync(session, token);
                
                token.ThrowIfCancellationRequested();
                await ReceiveDataAsync(session, header, token);
            }
        }

        protected virtual async Task ReadSendChannelAsync(CancellationToken token) {
            while (session.Connected) {
                token.ThrowIfCancellationRequested();
                var data = await _sendChannel.Reader.ReadAsync(token);
                
                token.ThrowIfCancellationRequested();
                await SendAsync(session, data, token);
            }
        }
        
        public abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
        public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
        
        public abstract Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;

        public bool Send<T>(uint sessionId, T data) where T : TData => session.ID == sessionId && _sendChannel.Writer.TryWrite(data);
        public bool Send<T>(TcpSession session, T data) where T : TData => Send<T>(session.ID, data);

        public async Task<bool> SendAsync<T>(uint sessionId, T data) where T : TData {
            if (session.ID == sessionId) {
                var task = _sendChannel.Writer.WriteAsync(data);
                await task;
                return task.IsCompletedSuccessfully;
            }

            return false;
        }

        public async Task<bool> SendAsync<T>(TcpSession session, T data) where T : TData => await SendAsync<T>(session.ID, data);
    }

    public abstract class SimpleTcpClient : IDisposable {

        public TcpClient Client;
        protected string host;
        protected int port;

        private const int CONNECT_TIME_OUT = 2500;

        public bool Connected => Client?.Connected ?? false;
        
        protected SimpleTcpClient(TcpClient client) => this.Client = client;

        public SimpleTcpClient(string host, int port) : this(new TcpClient()) {
            this.host = host;
            this.port = port;
        }
        
        ~SimpleTcpClient() {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Close();

        // public virtual bool Connect() {
        //     var result = client.BeginConnect(host, port, null, client);
        //     var handler = result.AsyncWaitHandle;
        //     if (handler.WaitOne(CONNECT_TIME_OUT, false)) {
        //         if (client.Connected == false || result.IsCompleted == false) {
        //             client.Close();
        //             return false;
        //         }
        //
        //         client.EndConnect(result);
        //         return true;
        //     }
        //
        //     client.Close();
        //     return false;
        // }

        public virtual async Task<bool> ConnectAsync() {
            var connectTask = Client.ConnectAsync(host, port);
            await connectTask;
            return connectTask.IsCompletedSuccessfully;
        }

        public Task Run() => throw new NotImplementedException();

        public virtual void Close() {
            if (Client is { Connected: true }) {
                Client.Close();
            }
        }

        public abstract Task StartTest(CancellationToken token);
    }
    
    internal class TestTcpJsonClient : SimpleTcpClient {
        
        public TestTcpJsonClient(string host, int port) : base(host, port) { }

        public override async Task StartTest(CancellationToken token) {
            if (Client.Connected) {
                var session = new TcpSession(Client, 4533u);
                var sessionData = new TcpJsonConnectSessionPacket {
                    sessionId = session.ID,
                }.ToBytes();
                
                var sessionHeader = new TcpHeader(session.ID, (int) TCP_BODY.CONNECT, sessionData.Length);
                await session.Stream.WriteAsync(sessionHeader.ToBytes(), token);
                await session.Stream.WriteAsync(sessionData, token);

                if (session.Connected) {
                    var responseHeader = await ReceiveHeaderAsync(session, token);
                    var response = await ReceiveAsync<TcpJsonResponseSessionPacket>(session, responseHeader, token);
                    if (response.IsValid() && response.isActive) {
                        _ = Task.Run(() => ReceiveAsync(session, token), token);

                        // Test Request
                        if (session.Connected) {
                            var data = new TcpJsonRequestTestPacket {
                                sessionId = session.ID,
                                requestText = "Hello World!!",
                            };
                            await SendAsync(session, data, token);
                        }
                    }
                }
            }
        }

        private async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                token.ThrowIfCancellationRequested();
                var buffer = owner.Memory;
                var bytesLength = await session.Stream.ReadAsync(buffer, token);
                if (bytesLength == 0) {
                    throw new DisconnectSessionException(session);
                }

                var responseHeader = buffer.ToStruct<TcpHeader>();
                if (responseHeader.HasValue) {
                    return responseHeader.Value;
                }

                throw new InvalidHeaderException();
            }
        }

        private async Task ReceiveAsync(TcpSession session, CancellationToken token) {
            while (token.IsCancellationRequested == false) {
                try {
                    var header = await ReceiveHeaderAsync(session, token);
                    if (TcpJsonHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
                        using (var owner = MemoryPool<byte>.Shared.Rent(1024))
                        await using (var memoryStream = new MemoryStream(header.length)) {
                            var totalBytesLength = 0;
                            var buffer = owner.Memory;
                            while (totalBytesLength < header.length) {
                                token.ThrowIfCancellationRequested();
                                var bytesLength = await session.Stream.ReadAsync(buffer, token);
                                if (bytesLength == 0) {
                                    throw new DisconnectSessionException(session);
                                }
                            
                                await memoryStream.WriteAsync(buffer[..bytesLength], token);
                                totalBytesLength += bytesLength;
                            }

                            await handler.ReceiveAsync(session, memoryStream.GetBuffer(), token);
                        }
                    }
                    
                    throw new InvalidHeaderException();
                } catch (Exception ex) {
                    Logger.TraceLog(ex, Color.red);
                }
            }
        }

        private async Task<TData> ReceiveAsync<TData>(TcpSession session, TcpHeader header, CancellationToken token) where TData : TcpJsonPacket, new() {
            if (header.TryGetEnumBody<TCP_BODY>(out var body) && TcpJsonHandlerProvider.inst.TryGetReceiveHandler<TData>(body, out var handler)) {
                using (var responseOwner = MemoryPool<byte>.Shared.Rent(1024))
                using (var memoryStream = new MemoryStream(header.length)) {
                    var totalBytesLength = 0;
                    var buffer = responseOwner.Memory;
                    while (totalBytesLength < header.length) {
                        token.ThrowIfCancellationRequested();
                        var bytesLength = await session.Stream.ReadAsync(buffer, token);
                        if (bytesLength == 0) {
                            throw new DisconnectSessionException(session);
                        }

                        await memoryStream.WriteAsync(buffer[..bytesLength], token);
                        totalBytesLength += bytesLength;
                    }
                    
                    var data = await handler.ReceiveBytesAsync(session, memoryStream.GetBuffer(), token);
                    return data;
                }
            }

            throw new InvalidHeaderException();
        }

        private async Task SendAsync<TData>(TcpSession session, TData data, CancellationToken token) where TData : ITcpPacket {
            if (session.Connected && TcpJsonHandlerProvider.inst.TryGetSendHandler<TData>(out var handler)) {
                await handler.SendDataAsync(session, data, token);
            }

            throw new NotImplementHandlerException<TCP_BODY>(data);
        }
    }

    internal class TestTcpPingClient : SimpleTcpClient {
        
        public TestTcpPingClient(string host, int port) : base(host, port) { }
        
        public override async Task StartTest(CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(32)) {
                while (token.IsCancellationRequested == false) {
                    var buffer = owner.Memory;
                    var length = await Client.GetStream().ReadAsync(buffer, token);
                    if (length == 0) {
                        throw new DisconnectSessionException(Client);
                    }

                    var textBytes = buffer[..length].ToArray();
                    Logger.TraceLog(textBytes.GetString());

                    var bytes = new byte[sizeof(bool)];
                    var value = true;
                    MemoryMarshal.Write(bytes, ref value);
                    await Client.GetStream().WriteAsync(bytes, token);
                }
            }
        }
    }
    
    internal class TestTcpStructClient : SimpleTcpClient {
        
        public TestTcpStructClient(string host, int port) : base(host, port) { }
        
        public override async Task StartTest(CancellationToken token) {
            if (Client.Connected) {
                var session = new TcpSession(Client, 9999u);
                var connect = new TcpRequestConnect(session);
                await session.Stream.WriteAsync(connect.ToBytes(), token);
                using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                    var buffer = owner.Memory;
                    var bytesLength = await session.Stream.ReadAsync(buffer, token);
                    if (bytesLength <= 0) {
                        throw new DisconnectSessionException(session);
                    }

                    var header = buffer.ToStruct<TcpHeader>();
                    if (header is { error: 0 }) {
                        _ = Task.Run(() => ReceiveAsync(session, token), token);
                        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpRequestTest>(out var handler)) {
                            await handler.SendDataAsync(session, new TcpRequestTest(15), token);
                        }
                    }
                }
            }
        }

        private async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                var buffer = owner.Memory;
                var bytesLength = await session.Stream.ReadAsync(buffer, token);
                if (bytesLength <= 0) {
                    throw new DisconnectSessionException(session);
                }

                var header = buffer.ToStruct<TcpHeader>();
                if (header.HasValue) {
                    return header.Value;
                }

                throw new InvalidHeaderException();
            }
        }

        private async Task ReceiveAsync(TcpSession session, CancellationToken token) {
            using (var owner = MemoryPool<byte>.Shared.Rent(1024)) {
                var buffer = owner.Memory;
                while (session.Connected) {
                     var header = await ReceiveHeaderAsync(session, token);
                    if (header.sessionId != session.ID) {
                        throw new InvalidDataException();
                    }

                    if (header.TryGetEnumBody<TCP_BODY>(out var body) && TcpStructHandlerProvider.inst.TryGetHandler(body, out var handler)) {
                        var totalReadLength = 0;
                        using (var memoryStream = new MemoryStream()) {
                            while (totalReadLength < header.length) {
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
    }
}
