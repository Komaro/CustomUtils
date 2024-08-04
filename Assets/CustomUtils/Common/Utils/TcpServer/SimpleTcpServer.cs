using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleTcpServer : IDisposable {

    private readonly TcpListener _listener;
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    
    private TcpServeModule _serveModule;

    private CancellationTokenSource _listenerCancelToken;
    private readonly ConcurrentDictionary<uint, TcpSession> _sessionDic = new();

    private bool _isRunning;
    
    private const int TEMP_PORT = 8890;
    
    public SimpleTcpServer() => _listener = new TcpListener(new IPEndPoint(IPAddress.Any, TEMP_PORT));
    public SimpleTcpServer(TcpServeModule serveModule) : this() => _serveModule = serveModule;

    public SimpleTcpServer(IPAddress ipAddress, int port) => _listener = new TcpListener(new IPEndPoint(ipAddress, port));
    public SimpleTcpServer(IPAddress ipAddress, int port, TcpServeModule serveModule) : this(ipAddress, port) => _serveModule = serveModule;

    ~SimpleTcpServer() => Dispose();
    
    public void Dispose() {
        if (_isRunning) {
            Stop();
            _listenerCancelToken.Cancel();
            _sessionDic.SafeClear(client => client.Dispose());
            _memoryPool.Dispose();
        }
    }

    public void Start() {
        if (IsRunning()) {
            Stop();
        }

        if (_serveModule == null) {
            Logger.TraceError($"{nameof(_serveModule)} is null");
            return;
        }
        
        _listener.Start(10);
        _isRunning = true;
        try {
            _listenerCancelToken = new CancellationTokenSource();
            _ = Task.Run(() => Run(_listenerCancelToken.Token), _listenerCancelToken.Token);
            Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Start)} || {_listener.LocalEndpoint}", Color.green);
        } catch (DisconnectSessionException ex) { }
        catch (Exception ex) {
            Logger.TraceError(ex);
            Stop();
        }
    }

    public void Stop() {
        _listener.Stop();
        _isRunning = false;
        Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Stop)}", Color.yellow);
    }
    
    private async void Run(CancellationToken token) {
        if (_serveModule == null) {
            throw new NoServeModuleException();
        }

        while (_isRunning) {
            var client = await _listener.AcceptTcpClientAsync();
            token.ThrowIfCancellationRequested();
            _ = Task.Run(() => ClientHandler(client, token), token);
        }
    }

    private async void ClientHandler(TcpClient client, CancellationToken token) {
        using (var session = await _serveModule.ConnectAsync(client, token)) {
            try {
                while (session.Connected && token.IsCancellationRequested == false) {
                    var header = await _serveModule.ReceiveHeaderAsync(session, token);
                    if (header.HasValue) {
                        await _serveModule.ReceiveDataAsync(session, header.Value, token);
                    }
                }
            } finally {
                if (session != null) {
                    _sessionDic.TryRemove(session.ID, out _);
                } else {
                    client.Dispose();
                }
            }
        }
    }

    public void ChangeServeModule(TcpServeModule serveModule) {
        if (_serveModule != null) {
            _serveModule.Close();
        }
        
        _serveModule = serveModule;
    }

    public bool TryGetSession(uint id, out TcpSession session) => (session = GetSession(id)) != null;
    public TcpSession GetSession(uint id) => _sessionDic.TryGetValue(id, out var session) ? session : null;

    public void AddOrUpdateSession(uint id, TcpSession session) => _sessionDic.AddOrUpdate(id, session, (_, oldSession) => {
        oldSession.Dispose();
        return session;
    });

    public bool IsRunning() => _isRunning;
    
    private class NoServeModuleException : SystemException {
    
        public NoServeModuleException() : base($"Currently, {nameof(TcpServeModule)} does not exist.") { }
    }
}

public sealed class TcpSession : IDisposable {

    public TcpClient Client { get; }
    public uint ID { get; }
    public bool Connected => Client.Connected;
    public NetworkStream Stream => Client.GetStream();

    ~TcpSession() => Dispose();

    public void Dispose() => Client?.Dispose();

    public TcpSession(TcpClient client, uint id) {
        Client = client;
        ID = id;
    }
}

public class DisconnectSessionException : Exception {

    private readonly string _address;
    private readonly uint _id;


    public DisconnectSessionException(TcpSession session) : this(session.Client, session.ID) { }
    public DisconnectSessionException(TcpClient client, uint id) : this(client) => _id = id;
    public DisconnectSessionException(TcpClient client) => _address = client.GetIpAddress();

    // TODO. Need Test
    public override string ToString() => $"Session Disconnected.\n[{nameof(IPEndPoint.Address)}] {_address}{(_id != 0 ? $"\n[Session] {_id}" : string.Empty)}";
}

public abstract class TcpResponseException<T> : Exception where T : struct {

    public virtual T CreateResponse() => default;
}


public class DuplicateSessionException : TcpResponseException<TcpResponseSession> {

    public override TcpResponseSession CreateResponse() => new(TCP_ERROR.DUPLICATE_SESSION);
}

public static class TcpExtension {

    public static string GetIpAddress(this TcpClient session) => session.Client != null ? session.Client.GetIpAddress() : string.Empty;
    public static string GetIpAddress(this Socket socket) => socket.RemoteEndPoint is IPEndPoint ipEndPoint ? ipEndPoint.Address.ToString() : string.Empty;
    public static string GetIpAddress(this EndPoint endPoint) => endPoint is IPEndPoint ipEndPoint ? ipEndPoint.Address.ToString() : string.Empty;
}