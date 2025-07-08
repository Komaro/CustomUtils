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

    public virtual bool Start(TcpListener listener, CancellationToken token) {
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
    
    // TODO. Session 기능으로 이전
    public virtual async Task<byte[]> ReadBytesAsync(TcpClient client, int length, CancellationToken token) {
        try {
            return await ReadBytesAsync(client.GetStream(), length, token);
        } catch (DisconnectSessionException) {
            throw new DisconnectSessionException(client);
        }
    }
    
    // TODO. Session 기능으로 이전
    public virtual async Task<byte[]> ReadBytesAsync(TcpSession session, int length, CancellationToken token) {
        try {
            return await ReadBytesAsync(session.Stream, length, token);
        } catch (DisconnectSessionException) {
            throw new DisconnectSessionException(session);
        }
    }

    // TODO. Session 기능으로 이전
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

/*
public abstract class TcpFixServeModule<THeader, TData> : TcpServeModule<THeader, TData> {

    protected readonly RecyclableMemoryStreamManager memoryStreamManager = new();
    
    protected readonly Channel<MemoryStream> memoryChannel = Channel.CreateUnbounded<MemoryStream>();
    protected readonly Channel<ReadOnlyMemory<byte>> channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();

    protected readonly MemoryStream bufferStream;
    
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();

    public TcpFixServeModule() => bufferStream = memoryStreamManager.GetStream();

    protected override async Task<bool> ConnectAsync(TcpClient client, CancellationToken token) {
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

                    _ = Task.Run(async () => {
                        await ReceiveAsync(session, token);
                        await ParseAsync(token);
                    }, token);
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

    protected override async Task ReceiveAsync(TcpSession session, CancellationToken token) {
        Logger.TraceLog($"Start {nameof(ReceiveAsync)} {nameof(Task)}", Color.LightGreen);
        using (var owner = memoryPool.Rent(RECEIVE_BUFFER_SIZE)) {
            var buffer = owner.Memory;
            while (session.Connected && token.IsCancellationRequested == false) {
                try {
                    var byteLength = await session.Stream.ReadAsync(buffer, token);
                    if (byteLength == 0) {
                        throw new DisconnectSessionException();
                    }

                    await channel.Writer.WriteAsync(buffer[..byteLength], token);

                    
                    
                    var stream = memoryStreamManager.GetStream("chunk");
                    await stream.WriteAsync(buffer[..byteLength], token);

                    await memoryChannel.Writer.WriteAsync(stream, token);
                } catch (DisconnectSessionException) {
                    Disconnect(session.ID);
                } catch (SocketException) {
                    Disconnect(session.ID);
                } catch (Exception ex) {
                    Logger.TraceLog(ex.Message);
                } finally {
                    memoryChannel.Writer.Complete();
                }
            }
        }
    }

    private ReadOnlySequence<byte> _sequence = new();

    protected virtual async Task ParseAsync(CancellationToken token) {
        while (token.IsCancellationRequested == false) {
            using var stream = await memoryChannel.Reader.ReadAsync(token);
            stream.Position = 0;
            

            var memory = stream.GetBuffer().AsMemory(0, (int) stream.Length);


            await stream.CopyToAsync(bufferStream, token);
            bufferStream.Position = 0;
            
            // TODO. HEADER, BODY 파싱 처리
            // TODO. 메소드 추출 필요
            if (bufferStream.Length - bufferStream.Position > TCP_HEADER_SIZE) {
                Memory<byte> headerBytes = new byte[TCP_HEADER_SIZE];
                var length = await bufferStream.ReadAsync(headerBytes, token);

                if (bufferStream.Length - bufferStream.Position > length) {
                    // TODO. BODY 처리
                } else {
                    bufferStream.Position -= TCP_HEADER_SIZE;
                }
            }
            
            // TODO. 읽고 남은 byte 처리
            // TODO. 메소드 추출 필요
            var remainBytes = stream.Length - stream.Position;
            if (remainBytes <= 0) {
                bufferStream.SetLength(0);
                bufferStream.Position = 0;
                continue;
            }

            var buffer = bufferStream.GetBuffer();
            Buffer.BlockCopy(buffer, (int)bufferStream.Position, buffer, 0, (int)remainBytes);
            bufferStream.SetLength(remainBytes);
            bufferStream.Position = 0;
            
            await stream.DisposeAsync();
        }
    }

    // TODO. NetworkStream을 직접 읽어들이지 않고 MemoryStream에서 읽어들이도록 시도 
    protected override async Task<byte[]> ReadBytesAsync(NetworkStream stream, int length, CancellationToken token) {
        using (var owner = memoryPool.Rent(length))
        using (var memoryStream = memoryStreamManager.GetStream("buffer", 1024)) {
            bufferStream.Position = 0;
            var buffer = owner.Memory;
            var totalBytesLength = 0;
            while (totalBytesLength < length) {
                var readBytes = await bufferStream.ReadAsync(buffer, token);
                if (readBytes <= 0) {
                    throw new DisconnectSessionException();
                }
                
                await memoryStream.WriteAsync(buffer[..readBytes], token);
                totalBytesLength += readBytes;
            }
            
            
        }

        throw new NotImplementedException();
    }
}*/