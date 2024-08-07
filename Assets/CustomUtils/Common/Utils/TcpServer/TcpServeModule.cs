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
    public abstract Task<TcpHeader?> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
    public abstract Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token);

    public virtual async Task Send<T>(TcpSession session, T structure, CancellationToken token) where T : struct, ITcpStructure {
        if (structure.IsValid() && TcpHandlerProvider.TryGetSendHandler<T>(out var handler)) {
            await handler.SendAsync(session, structure, token);
        }
    }
    
    public abstract void Close();
}

public class TcpSimpleServeModule : TcpServeModule {

    public readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    
    private static readonly int TCP_CONNECT_SIZE = Marshal.SizeOf<TcpRequestConnect>();
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();

    public TcpSimpleServeModule(SimpleTcpServer server) : base(server) {
        
    }

    public override async Task<TcpSession> ConnectAsync(TcpClient client, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(TCP_CONNECT_SIZE)) {
            var stream = client.GetStream();
            var buffer = memoryOwner.Memory;
            var bytesLength = await stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(client);
            }
            
            token.ThrowIfCancellationRequested();
            
            var data = buffer.ToStruct<TcpRequestConnect>();
            if (data.HasValue) {
                if (data.Value.IsValid() == false) {
                    throw await TcpHandlerProvider.AsyncResponseException(new InvalidSessionData(data.Value), client, token);
                }
            
                var id = data.Value.sessionId;
                if (server.TryGetSession(id, out var session) && session.Connected) {
                    throw await TcpHandlerProvider.AsyncResponseException(new DuplicateSessionException(), client, token);
                }

                var newSession = new TcpSession(client, id);
                server.AddOrUpdateSession(id, newSession);
                
                try {
                    var header = new TcpResponseConnect(true);
                    if (header.TryBytes(out var bytes)) {
                        await stream.WriteAsync(bytes, token);
                        token.ThrowIfCancellationRequested();
                    }
                } catch {
                    throw new InvalidCastException($"Failed to convert {nameof(TcpError)} to byte[]");
                }
                
                Logger.TraceLog($"Connected || {id} || {client.Client.RemoteEndPoint}", Color.green);
                return newSession;
            }

            throw new InvalidCastException($"Failed to convert {nameof(Memory<byte>)} to {nameof(TcpRequestConnect)}");
        }
    }

    public override async Task<TcpHeader?> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(TCP_HEADER_SIZE)) {
            var buffer = memoryOwner.Memory;
            var bytesLength = await session.Stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(session);
            }
            
            token.ThrowIfCancellationRequested();
            return buffer.ToStruct<TcpHeader>();
        }
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        using (var memoryOwner = _memoryPool.Rent(1024)) {
            using var memoryStream = new MemoryStream();
            var buffer = memoryOwner.Memory;
            var totalReadBytesLength = 0;
            while (totalReadBytesLength < header.byteLength) {
                var readBytes = await session.Stream.ReadAsync(buffer, token);
                if (readBytes == 0) {
                    throw new DisconnectSessionException(session);
                }
                
                token.ThrowIfCancellationRequested();

                await memoryStream.WriteAsync(buffer[..readBytes], token);
                totalReadBytesLength += readBytes;
            }
            
            if (header.bodyType != TCP_BODY.NONE && TcpHandlerProvider.TryGetReceiveHandler(header.bodyType, out var handler)) {
                await handler.ReceiveAsync(session, memoryStream.ToArray(), token);
            }
            
            await memoryStream.DisposeAsync();
        }
    }

    public override void Close() => _memoryPool.Dispose();
}

