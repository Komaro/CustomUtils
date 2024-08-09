using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TcpStructServeModule : TcpServeModule<TcpHeader, ITcpPacket> {
    
    private static readonly int TCP_CONNECT_SIZE = Marshal.SizeOf<TcpRequestConnect>();
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        using (var memoryOwner = memoryPool.Rent(TCP_CONNECT_SIZE)) {
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
                    throw await TcpStructHandlerProvider.AsyncResponseException(new InvalidSessionData(data.Value), client, token);
                }
            
                var id = data.Value.sessionId;
                if (sessionDic.TryGetValue(id, out var session) && session.Connected) {
                    throw await TcpStructHandlerProvider.AsyncResponseException(new DuplicateSessionException(), client, token);
                }
                
                var newSession = new TcpSession(client, id);
                sessionDic.AddOrUpdate(id, newSession, (_, oldSession) => {
                    oldSession.Close();
                    return newSession;
                });

                try {
                    var header = new TcpResponseConnect(true);
                    if (header.TryBytes(out var bytes)) {
                        await stream.WriteAsync(bytes, token);
                        token.ThrowIfCancellationRequested();
                    }
                } catch {
                    Logger.Error($"Failed to convert {nameof(TcpError)} to byte[]");
                    return null;
                }
                
                Logger.TraceLog($"Connected || {id} || {client.Client.RemoteEndPoint}", Color.green);
                return newSession;
            }

            Logger.Error($"Failed to convert {nameof(Memory<byte>)} to {nameof(TcpRequestConnect)}");
            return null;
        }
    }

    public override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        using (var memoryOwner = memoryPool.Rent(TCP_HEADER_SIZE)) {
            var buffer = memoryOwner.Memory;
            var bytesLength = await session.Stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(session);
            }
            
            token.ThrowIfCancellationRequested();

            var header = buffer.ToStruct<TcpHeader>();
            return header ?? default;
        }
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        if (header.IsValid() == false) {
            return;
        }
    
        using (var memoryOwner = memoryPool.Rent(1024)) {
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
            
            if (header.bodyType != TCP_STRUCT_BODY.NONE && TcpStructHandlerProvider.TryGetReceiveHandler(header.bodyType, out var handler)) {
                await handler.ReceiveAsync(session, memoryStream.ToArray(), token);
            }
            
            await memoryStream.DisposeAsync();
        }
    }

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (data.IsValid() && TcpStructHandlerProvider.TryGetSendHandler<T>(out var handler)) {
            await handler.SendAsync(session, data, token);
            return true;
        }

        return false;
    }
}