using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Drawing;
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
    protected readonly Channel<(TcpSession, TData)> sendChannel = Channel.CreateBounded<(TcpSession, TData)>(new BoundedChannelOptions(50));
    
    protected CancellationTokenSource cancelToken;

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
            
            cancelToken = new CancellationTokenSource();
            Task.Run(() => ReadConnectChannelAsync(cancelToken.Token), cancelToken.Token);
            Task.Run(() => ReadSendChannelAsync(cancelToken.Token), cancelToken.Token);
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
        sessionDic.SafeClear(session => session.Close());
        cancelToken?.Cancel();
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
                await connectChannel.Writer.WriteAsync((client, token), cancelToken.Token);
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
        token.ThrowIfCancellationRequested();
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
        } catch(Exception ex){
            Logger.TraceLog(ex);
            client.Close();
            return false;
        }
        
        return false;
    }

    public void Disconnect(TcpSession session) => Disconnect(session.ID);

    public void Disconnect(uint sessionId) {
        if (sessionDic.TryRemove(sessionId, out var session) && session.IsValid()) {
            Logger.TraceLog($"{nameof(Disconnect)} {nameof(TcpSession)} || {session.ID}", Color.Red);
            session.Close();
        }
    }

    protected virtual async Task ReceiveAsync(TcpSession session, CancellationToken token) {
        Logger.TraceLog($"Start {nameof(ReceiveAsync)} {nameof(Task)}", Color.LightGreen);
        while (session.Connected && token.IsCancellationRequested == false) {
            try {
                var header = await ReceiveHeaderAsync(session, token);
                token.ThrowIfCancellationRequested();

                await ReceiveDataAsync(session, header, token);
                token.ThrowIfCancellationRequested();
            } catch (DisconnectSessionException) {
                Disconnect(session.ID);
            } catch (SocketException) {
                Disconnect(session.ID);
            } catch (Exception ex) {
                Logger.TraceLog(ex.Message);
            }
        }
    }
    
    private async Task ReadConnectChannelAsync(CancellationToken channelToken) {
        Logger.TraceLog($"Start {nameof(ReadConnectChannelAsync)} {nameof(Task)}", Color.LightGreen);
        while (IsRunning()) {
            var (client, token) = await connectChannel.Reader.ReadAsync(channelToken);
            channelToken.ThrowIfCancellationRequested();
            if (await ConnectAsync(client, token) == false) {
                Logger.TraceError($"Failed to connect to");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ReadSendChannelAsync(CancellationToken channelToken) {
        Logger.TraceLog($"Start {nameof(ReadSendChannelAsync)} {nameof(Task)}", Color.LightGreen);
        while (IsRunning()) {
            var (session, data) = await sendChannel.Reader.ReadAsync(channelToken);
            channelToken.ThrowIfCancellationRequested();
            if (await SendAsync(session, data, channelToken) == false) {
                Logger.TraceError($"Failed to send to {session.ID} [{data.GetType().Name}]");
            }
        }
        
        await Task.CompletedTask;
    }

    public abstract Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
    
    public virtual async Task<byte[]> ReadBytesAsync(TcpClient client, int length, CancellationToken token) {
        try {
            return await ReadBytesAsync(client.GetStream(), length, token);
        } catch (DisconnectSessionException) {
            throw new DisconnectSessionException(client);
        }
    }
    /*
     *
     */
    public virtual async Task<byte[]> ReadBytesAsync(TcpSession session, int length, CancellationToken token) {
        try {
            return await ReadBytesAsync(session.Stream, length, token);
        } catch (DisconnectSessionException) {
            throw new DisconnectSessionException(session);
        }
    }

    protected virtual async Task<byte[]> ReadBytesAsync(NetworkStream stream, int length, CancellationToken token) {
        using (var owner = memoryPool.Rent(length))
        using (var memoryStream = new MemoryStream(length)) {
            var buffer = owner.Memory;
            var totalBytesLength = 0;
            while (totalBytesLength < length) {
                var bytesLength = await stream.ReadAsync(buffer, token);
                if (bytesLength == 0) {
                    throw new DisconnectSessionException();
                }
                
                token.ThrowIfCancellationRequested();
                
                await memoryStream.WriteAsync(buffer[..bytesLength], token);
                totalBytesLength += bytesLength;
            }

            return memoryStream.GetBuffer();
        }
    }
    
    public abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
    public abstract Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;

    public bool Send<T>(uint sessionId, T data) where T : TData => sessionDic.TryGetValue(sessionId, out var session) && Send(session, data);
    public virtual bool Send<T>(TcpSession session, T data) where T : TData => sendChannel.Writer.TryWrite((session, data));

    public async Task<bool> SendAsync<T>(uint sessionId, T data) where T : TData {
        if (sessionDic.TryGetValue(sessionId, out var session)) {
            return await SendAsync(session, data);
        }

        return false;
    }
    
    public virtual async Task<bool> SendAsync<T>(TcpSession session, T data) where T : TData {
        var task = sendChannel.Writer.WriteAsync((session, data));
        await task;
        return task.IsCompletedSuccessfully;
    }

    public bool IsRunning() => isRunning;
}