using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public interface ITcpServeModule {
    
    public bool Start(TcpListener listener, CancellationToken token);
    public void Stop();
    public void Close();
    
    public Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
}

public interface ITcpReceiveModule<THeader> {
    
    public Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
}

public interface ITcpSendModule<in TData> {

    public Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;

    public bool Send<T>(uint sessionId, T data) where T : TData;
    public bool Send<T>(TcpSession session, T data) where T : TData;
    
    public Task<bool> SendAsync<T>(uint sessionId, T data) where T : TData;
    public Task<bool> SendAsync<T>(TcpSession session, T data) where T : TData;
}

public abstract class TcpServeModule<THeader, TData> : ITcpServeModule, ITcpReceiveModule<THeader>, ITcpSendModule<TData>, IDisposable {

    protected TcpListener listener;
    
    protected readonly MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;
    protected readonly ConcurrentDictionary<uint, TcpSession> sessionDic = new();

    protected readonly Channel<(TcpClient, CancellationToken)> connectChannel = Channel.CreateBounded<(TcpClient, CancellationToken)>(new BoundedChannelOptions(5));
    protected readonly Channel<(TcpSession, TData)> _sendChannel = Channel.CreateBounded<(TcpSession, TData)>(new BoundedChannelOptions(50));
    
    protected CancellationTokenSource channelCancelToken;

    protected bool isRunning;

    protected virtual int RECEIVE_BUFFER_SIZE => 1024; 

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
            Task.Run(() => ServeAsync(token), token);
            
            channelCancelToken = new CancellationTokenSource();
            for (var i = 0; i < 5; i++) {
                Task.Run(() => ReadConnectChannelAsync(channelCancelToken.Token), channelCancelToken.Token);
                Task.Run(() => ReadSendChannelAsync(channelCancelToken.Token), channelCancelToken.Token);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Close();
            return false;
        }
        
        isRunning = true;
        return true;
    }

    public virtual void Stop() {
        isRunning = false;
        channelCancelToken?.Cancel();
    }

    public virtual void Close() {
        if (IsRunning()) {
            Stop();
        }
        
        memoryPool?.Dispose();
    }
    
    protected virtual async Task ServeAsync(CancellationToken token) {
        while (token.IsCancellationRequested == false) {
            try {
                var client = await AcceptAsync(token);
                await connectChannel.Writer.WriteAsync((client, token), channelCancelToken.Token);
            }
            catch (OperationCanceledException) { }
            catch (SocketException ex) {
                Logger.TraceLog($"{ex.SocketErrorCode} || {ex.Message}");
            } catch (SystemException ex) {
                Logger.TraceLog(ex);
            } 
        }
    }

    protected virtual async Task<TcpClient> AcceptAsync(CancellationToken token) {
        token.ThrowIfCancellationRequested();
        var client = await listener.AcceptTcpClientAsync();
        return client;
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
    
    private async Task ReadConnectChannelAsync(CancellationToken channelToken) {
        while (IsRunning()) {
            channelToken.ThrowIfCancellationRequested();
            var (client, token) = await connectChannel.Reader.ReadAsync(channelToken);
            if (await ConnectAsync(client, token) == false) {
                Logger.TraceError($"Failed to connect to {client.GetIpAddress()}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ReadSendChannelAsync(CancellationToken channelToken) {
        while (IsRunning()) {
            var (session, data) = await _sendChannel.Reader.ReadAsync(channelToken);
            channelToken.ThrowIfCancellationRequested();
            if (await SendAsync(session, data, channelToken) == false) {
                Logger.TraceError($"Failed to send to {session.ID} [{data.GetType().Name}]");
            }
        }
        
        await Task.CompletedTask;
    }

    public abstract Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
    
    public virtual async Task<byte[]> ReceiveBytesAsync(TcpClient client, int length, CancellationToken token) {
        using (var owner = memoryPool.Rent(RECEIVE_BUFFER_SIZE))
        using (var stream = new MemoryStream(length)) {
            var buffer = owner.Memory;
            var totalBytesLength = 0;
            while (totalBytesLength < length) {
                var bytesLength = await client.GetStream().ReadAsync(buffer, token);
                if (bytesLength == 0) {
                    throw new DisconnectSessionException(client);
                }
                
                token.ThrowIfCancellationRequested();
                
                await stream.WriteAsync(buffer[..bytesLength], token);
                totalBytesLength += bytesLength;
            }

            return stream.GetBuffer();
        }
    }
    
    public virtual async Task<byte[]> ReceiveBytesAsync(TcpSession session, int length, CancellationToken token) {
        using (var owner = memoryPool.Rent(RECEIVE_BUFFER_SIZE))
        using (var stream = new MemoryStream(length)) {
            var buffer = owner.Memory;
            var totalBytesLength = 0;
            while (totalBytesLength < length) {
                var bytesLength = await session.Stream.ReadAsync(buffer, token);
                if (bytesLength == 0) {
                    throw new DisconnectSessionException(session);
                }
                
                token.ThrowIfCancellationRequested();
                
                await stream.WriteAsync(buffer[..bytesLength], token);
                totalBytesLength += bytesLength;
            }

            return stream.GetBuffer();
        }
    }
    public abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
    public abstract Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;

    public bool Send<T>(uint sessionId, T data) where T : TData => sessionDic.TryGetValue(sessionId, out var session) && _sendChannel.Writer.TryWrite((session, data));
    public bool Send<T>(TcpSession session, T data) where T : TData => _sendChannel.Writer.TryWrite((session, data));

    public async Task<bool> SendAsync<T>(uint sessionId, T data) where T : TData {
        if (sessionDic.TryGetValue(sessionId, out var session)) {
            var task = _sendChannel.Writer.WriteAsync((session, data));
            await task;
            return task.IsCompletedSuccessfully;
        }

        return false;
    }    
    
    public async Task<bool> SendAsync<T>(TcpSession session, T data) where T : TData {
        var task = _sendChannel.Writer.WriteAsync((session, data));
        await task;
        return task.IsCompletedSuccessfully;
    }

    public bool IsRunning() => isRunning;
}