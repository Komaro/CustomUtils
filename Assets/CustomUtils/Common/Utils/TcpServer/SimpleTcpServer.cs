using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

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
                Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Start)} || {_listener.LocalEndpoint}", Color.green);
            } else {
                throw new InvalidServeModuleException();
            }
        } catch (DisconnectSessionException) { }
        catch (Exception ex) {
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
                Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Stop)}", Color.red);
            } catch (Exception ex) {
                Logger.TraceError(ex);
            }
        } else {
            Logger.TraceLog($"{nameof(SimpleTcpServer)} is already stopped", Color.yellow);
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
    public uint ID { get; }
    public bool Connected => IsValid() && Client.Connected;
    public NetworkStream Stream => IsValid() ? Client.GetStream() : null;

    public TcpSession(TcpClient client) {
        Client = client;
        ID = 0;
    }

    public TcpSession(TcpClient client, uint id) {
        Client = client;
        ID = id;
    }
    
    ~TcpSession() => Dispose();
    
    public void Dispose() => Close();
    public void Close() => Client?.Close();

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