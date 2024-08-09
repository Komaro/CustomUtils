using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public interface ITcpServeModule {
    
    public bool Start(TcpListener listener, CancellationToken token);
    public void Close();
    
    public Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
}

public interface ITcpReceiveModule<THeader> : ITcpServeModule {
    
    public Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
}

public interface ITcpSendModule<in TData> : ITcpServeModule {

    public Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;
    public void Send<T>(uint sessionId, T data) where T : TData;
    public void Send<T>(TcpSession session, T data) where T : TData;
}

public abstract class TcpServeModule<THeader, TData> : ITcpReceiveModule<THeader>, ITcpSendModule<TData>, IDisposable {

    protected TcpListener listener;
    protected Task serveTask;
    
    protected readonly MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;
    protected readonly ConcurrentDictionary<uint, TcpSession> sessionDic = new();

    protected readonly Channel<(TcpClient, CancellationToken)> connectChannel = Channel.CreateBounded<(TcpClient, CancellationToken)>(new BoundedChannelOptions(5));
    protected Task readConnectTask;
    
    protected readonly Channel<(TcpSession, TData)> _sendChannel = Channel.CreateBounded<(TcpSession, TData)>(new BoundedChannelOptions(50));
    protected Task readSendTask;
    
    protected CancellationTokenSource channelToken;

    ~TcpServeModule() => Dispose();
    public void Dispose() => Close();

    public bool Start(TcpListener listener, CancellationToken token) {
        if (listener == null) {
            Logger.TraceError($"The {nameof(listener)} is null");
            return false;
        }

        if (IsRunning()) {
            Stop();
        }

        this.listener = listener;
        try {
            serveTask = Task.Run(() => ServeAsync(token), token);
            
            channelToken = new CancellationTokenSource();
            readConnectTask = Task.Run(async () => await ReadConnectChannelAsync(), channelToken.Token);
            readSendTask = Task.Run(async () => await ReadSendChannelAsync(), channelToken.Token);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Close();
            return false;
        }
        
        return true;
    }

    public virtual void Stop() {
        channelToken?.Cancel();
        
        serveTask?.Dispose();
        readConnectTask?.Dispose();
        readSendTask?.Dispose();
    }

    public virtual void Close() {
        if (IsRunning()) {
            Stop();
        }
        
        memoryPool?.Dispose();
    }

    
    protected virtual async Task ServeAsync(CancellationToken token) {
        try {
            while (token.IsCancellationRequested == false) {
                var client = await listener.AcceptTcpClientAsync();
                token.ThrowIfCancellationRequested();
                await connectChannel.Writer.WriteAsync((client, token), channelToken.Token);
            }
        } catch (OperationCanceledException) { }
    }

    protected virtual async Task<bool> ConnectAsync(TcpClient client, CancellationToken token) {
        try {
            var connectCount = 0;
            var maxReconnectCount = 5;
            while (token.IsCancellationRequested == false && client.Connected && connectCount < maxReconnectCount) {
                try {
                    var session = await ConnectSessionAsync(client, token);
                    connectCount++;
                    if (session == null) {
                        continue;
                    }

                    _ = Task.Run(async () => await ReceiveAsync(session, token), token);
                } catch (DisconnectSessionException) {
                    continue;
                } catch (InvalidCastException) {
                    continue;
                }

                return true;
            }
        } catch (SocketException ex) {
            Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
        } catch(Exception){
            client.Dispose();
            return false;
        }
        
        return false;
    }

    protected virtual async Task ReceiveAsync(TcpSession session, CancellationToken token) {
        while (session.Connected && token.IsCancellationRequested == false) {
            var header = await ReceiveHeaderAsync(session, token);
            token.ThrowIfCancellationRequested();
            
            await ReceiveDataAsync(session, header, token);
            token.ThrowIfCancellationRequested();
        }
    }
    
    private async Task ReadConnectChannelAsync() {
        while (IsRunning()) {
            var (client, token) = await connectChannel.Reader.ReadAsync();
            if (await ConnectAsync(client, token) == false) {
                Logger.TraceError($"Failed to connect to {client.GetIpAddress()}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ReadSendChannelAsync() {
        while (IsRunning()) {
            var (session, data) = await _sendChannel.Reader.ReadAsync();
            if (await SendAsync(session, data, channelToken.Token) == false) {
                Logger.TraceError($"Failed to send to {session.ID} [{data.GetType().Name}]");
            }
        }
        
        await Task.CompletedTask;
    }
    
    public abstract Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
    public abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
    public abstract Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;

    public void Send<T>(uint sessionId, T data) where T : TData {
        if (sessionDic.TryGetValue(sessionId, out var session)) {
            _sendChannel.Writer.WriteAsync((session, data));
        }
    }    
    
    public void Send<T>(TcpSession session, T data) where T : TData => _sendChannel.Writer.WriteAsync((session, data));

    public bool IsRunning() => serveTask is { IsCanceled: false, IsCompleted: false };
}