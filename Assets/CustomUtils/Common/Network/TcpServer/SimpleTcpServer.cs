using System;
using System.Buffers;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;

public class SimpleTcpServer : IDisposable {

    private TcpListener _listener;
    private IPAddress _ipAddress;
    private int _port;
    
    private ITcpServeModule _serveModule;

    private CancellationTokenSource _listenerCancelToken;

    private bool _isRunning;
    
    private const int TEMP_PORT = 8890;
    
    public SimpleTcpServer(IPAddress ipAddress, int port) {
        _ipAddress = ipAddress;
        _port = port;
    }
    
    public SimpleTcpServer(ITcpServeModule serveModule) : this(IPAddress.Any, TEMP_PORT) => _serveModule = serveModule;
    public SimpleTcpServer(IPAddress ipAddress, int port, ITcpServeModule serveModule) : this(ipAddress, port) => _serveModule = serveModule;

    ~SimpleTcpServer() => Dispose();
    
    public void Dispose() {
        if (_isRunning) {
            Stop();
        }
        
        GC.SuppressFinalize(this);
    }

    public void Start() {
        if (IsRunning()) {
            Stop();
        }

        if (_serveModule == null) {
            throw new NoServeModuleException();
        }

        _listener = new TcpListener(_ipAddress ?? IPAddress.Any, _port <= 0 ? TEMP_PORT : _port);
        _listener.Start(10);
        _isRunning = true;
        try {
            _listenerCancelToken = new CancellationTokenSource();
            if (_serveModule.Start(_listener, _listenerCancelToken.Token)) {
                Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Start)} || {_listener.LocalEndpoint}", Color.GreenYellow);
            } else {
                throw new InvalidServeModuleException();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Stop();
        }
    }

    public void Stop() {
        if (_isRunning) {
            try {
                _isRunning = false;
                _listenerCancelToken.Cancel();
                _serveModule.Stop();
                _listener.Stop();
                Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Stop)}", Color.Red);
            } catch (Exception ex) {
                Logger.TraceError(ex);
            }
        } else {
            Logger.TraceLog($"{nameof(SimpleTcpServer)} is already stopped", Color.Yellow);
        }
    }

    public void Send<TData>(TcpSession session, TData data) {
        if (_serveModule is ITcpSendModule<TData> sendModule) {
            sendModule.Send(session, data);
        }
    }
    
    public void ChangeServeModule(ITcpServeModule serveModule) {
        _serveModule?.Close();
        _serveModule = serveModule;
    }

    public bool IsRunning() => _isRunning;

    private class InvalidServeModuleException : SystemException { }
    
    private class NoServeModuleException : SystemException {
    
        public NoServeModuleException() : base($"Currently, {nameof(ITcpServeModule)} does not exist.") { }
    }
}

public class TcpSession : IDisposable {

    public TcpClient Client { get; }
    public uint ID { get; internal set; }
    public bool Connected => IsValid() && Client.Connected;

    // TODO. Stream 노출을 제거할 필요가 있음
    public NetworkStream Stream => IsValid() ? Client.GetStream() : null;

    private readonly IMemoryOwner<byte> _buffer;
    private readonly IMemoryOwner<byte> _leftBuffer;
    private int _leftLength;
    
    private readonly RecyclableMemoryStreamManager _manager;

    private TcpSession() {
        _buffer = MemoryPool<byte>.Shared.Rent(1024);
        _leftBuffer = MemoryPool<byte>.Shared.Rent(1024);
        _manager = new RecyclableMemoryStreamManager();
    }
    
    public TcpSession(TcpClient client) : this() {
        Client = client;
        ID = 0;
    }

    public TcpSession(TcpClient client, uint id) : this() {
        Client = client;
        ID = id;
    }
    
    ~TcpSession() => Dispose();
    
    public void Dispose() => Close();

    public void Close() => Client?.Close();
    
    public async Task<Memory<byte>> ReadBytesAsync(int length, CancellationToken token) {
        using var readStream = _manager.GetStream("readStream");
        var totalBytes = 0;
        if (_leftLength > 0) {
            var leftBuffer = _leftBuffer.Memory;
            var maxReadLength = Math.Min(_leftLength, length);
            await readStream.WriteAsync(leftBuffer[..maxReadLength], token);
            totalBytes += maxReadLength;

            var remainLength = _leftLength - maxReadLength;
            if (remainLength > 0) {
                leftBuffer.Slice(maxReadLength, remainLength).CopyTo(leftBuffer[..remainLength]);
            }
            
            _leftLength = remainLength; 
        }

        var buffer = _buffer.Memory;
        while (totalBytes < length) {
            var readBytes = await Client.GetStream().ReadAsync(buffer, token);
            if (readBytes == 0) {
                throw new DisconnectSessionException();
            }
            
            token.ThrowIfCancellationRequested();
            await readStream.WriteAsync(buffer, token);
            totalBytes += readBytes;
        }
        
        var bytes = readStream.GetBuffer().AsMemory();
        if (totalBytes > length) {
            _leftLength = totalBytes - length;
            bytes.Slice(length, _leftLength).CopyTo(_leftBuffer.Memory);
        }

        return bytes[..length];
    }
    
    // TODO. SendAsync
    public virtual async Task SendAsync() {
        
    }


    public bool IsValid() => Client != null;
    public bool VerifySession(uint id) => ID == id;
}

public enum TCP_ERROR {
    NONE = 0,
    
    // Session
    DUPLICATE_SESSION = 100,
    INVALID_SESSION_DATA = 101,
    
    // Data
    MISSING_DATA = 200,
    
    // Progress
    EXCEPTION_PROGRESS = 300,
    
    INVALID_TEST_COUNT = 1000,
}