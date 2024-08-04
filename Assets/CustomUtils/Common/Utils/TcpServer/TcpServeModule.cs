using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class TcpServeModule {

    protected SimpleTcpServer server;

    public TcpServeModule(SimpleTcpServer server) => this.server = server;

    public abstract Task<TcpSession> ConnectAsync(TcpClient client, CancellationToken token);
    public abstract Task<TcpSendHeader?> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public abstract Task ReceiveDataAsync(TcpSession session, TcpSendHeader header, CancellationToken token);
    
    public abstract void SendHeader(TcpSession session);
    public abstract void Send(TcpSession session);

    public abstract void Close();
}

public class TcpSimpleServeModule : TcpServeModule {

    public readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    
    private static readonly int SEND_SESSION_SIZE = Marshal.SizeOf<TcpSendSession>();
    private static readonly int RECEIVE_SESSION_SIZE = Marshal.SizeOf<TcpResponseSession>();

    private static readonly int SEND_HEADER_SIZE = Marshal.SizeOf<TcpSendHeader>();
    private static readonly int RECEIVE_HEADER_SIZE = Marshal.SizeOf<TcpResponseHeader>();

    public TcpSimpleServeModule(SimpleTcpServer server) : base(server) {
    
    }

    public override async Task<TcpSession> ConnectAsync(TcpClient client, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(SEND_SESSION_SIZE)) {
            await using var stream = client.GetStream();
            var buffer = memoryOwner.Memory;
            var bytesLength = await stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(client);
            }
            
            token.ThrowIfCancellationRequested();
            
            var sendSession = buffer.ToStruct<TcpSendSession>();
            if (sendSession.HasValue) {
                var id = sendSession.Value.sessionId;
                if (server.TryGetSession(id, out var session) && session.Connected) {
                    throw await AsyncResponseException(new DuplicateSessionException(), stream, token);
                }

                var newSession = new TcpSession(client, id);
                server.AddOrUpdateSession(id, newSession);
                
                try {
                    var responseSession = new TcpResponseSession();
                    if (responseSession.TryBytes(out var bytes)) {
                        await stream.WriteAsync(bytes, token);
                        token.ThrowIfCancellationRequested();
                    }
                } catch {
                    throw new InvalidCastException($"Failed to convert {nameof(TcpResponseSession)} to byte[]");
                }
                
                Logger.TraceLog($"Connected || {id} || {client.Client.RemoteEndPoint}", Color.green);
                return newSession;
            }

            throw new InvalidCastException($"Failed to convert {nameof(Memory<byte>)} to {nameof(TcpSendSession)}");
        }
    }

    public override async Task<TcpSendHeader?> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(SEND_HEADER_SIZE)) {
            var buffer = memoryOwner.Memory;
            var bytesLength = await session.Stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(session);
            }
            
            token.ThrowIfCancellationRequested();
            return buffer.ToStruct<TcpSendHeader>();
        }
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpSendHeader header, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(1024)) {
            using var memoryStream = new MemoryStream();
            var buffer = memoryOwner.Memory;
            var totalReadBytesLength = 0;
            while (totalReadBytesLength < header.dataLength) {
                var readBytes = await session.Stream.ReadAsync(buffer, token);
                if (readBytes == 0) {
                    throw new DisconnectSessionException(session);
                }
                
                token.ThrowIfCancellationRequested();

                await memoryStream.WriteAsync(buffer[..readBytes], token);
                totalReadBytesLength += readBytes;
            }
                    
            var bytes = memoryStream.ToArray();
            Logger.TraceLog(Encoding.UTF8.GetString(bytes));

            await memoryStream.DisposeAsync();
        }
    }

    public override void SendHeader(TcpSession session) {
        throw new System.NotImplementedException();
    }

    public override void Send(TcpSession session) {
        throw new System.NotImplementedException();
    }

    public override void Close() {
        throw new System.NotImplementedException();
    }
    
    private async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, Stream stream, CancellationToken token) where T : struct {
        var response = exception.CreateResponse();
        await stream.WriteAsync(response.ToBytes(), token);
        return exception;
    }
}

