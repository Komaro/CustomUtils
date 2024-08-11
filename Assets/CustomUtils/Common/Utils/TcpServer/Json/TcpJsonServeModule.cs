using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TcpJsonServeModule : TcpServeModule<TcpHeader, IJsonData> {

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    
    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        using (var headerOwner = memoryPool.Rent(TCP_HEADER_SIZE)) {
            var buffer = headerOwner.Memory;
            var bytesLength = await client.GetStream().ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(client);
            }
            
            var requestHeader = buffer.ToStruct<TcpHeader>();
            if (requestHeader.HasValue) {
                using (var connectOwner = memoryPool.Rent(requestHeader.Value.byteLength)) {
                    buffer = connectOwner.Memory;
                    bytesLength = await client.GetStream().ReadAsync(buffer, token);
                    if (bytesLength == 0) {
                        throw new DisconnectSessionException(client);
                    }

                    var json = buffer.GetString();
                    var requestSession = JsonConvert.DeserializeObject<TcpJsonRequestSessionPacket>(json);
                    if (requestSession?.IsValid() ?? false) {
                        if (sessionDic.TryGetValue(requestSession.sessionId, out var session) && session.Connected) {
                            // TODO. Add Exception Response
                            throw await TcpExceptionProvider.ResponseExceptionAsync(new DuplicateSessionException(), session, token);
                        }
                        
                        session = new TcpSession(client, requestSession.sessionId);
                        sessionDic.AddOrUpdate(requestSession.sessionId, session, (_, oldSession) => {
                            oldSession.Close();
                            return session;
                        });

                        try {
                            var responseBytes = new TcpJsonResponseSessionPacket(true).ToBytes(); 
                            
                            var responseHeader = new TcpHeader(session, TCP_BODY.CONNECT, responseBytes.Length);
                            var headerBytes = responseHeader.ToBytes();
                            
                            await session.Stream.WriteAsync(headerBytes, token);
                            token.ThrowIfCancellationRequested();
                            
                            await session.Stream.WriteAsync(responseBytes, token);
                            token.ThrowIfCancellationRequested();
                        } catch {
                            Logger.Error($"Failed to convert {nameof(TcpError)} to byte[]");
                            return null;
                        }
                    }
                }
            }

            throw new SocketException((int)SocketError.AccessDenied);
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
        if (header.IsValid() && header.bodyType != default) {
            using (var owner = memoryPool.Rent(RECEIVE_BUFFER_SIZE)) {
                using var memoryStream = new MemoryStream(header.byteLength);
                var buffer = owner.Memory;
                var totalBytesLength = 0;
                while (totalBytesLength < header.byteLength) {
                    var bytesLength = await session.Stream.ReadAsync(buffer, token);
                    if (bytesLength == 0) {
                        throw new DisconnectSessionException(session);
                    }
                    
                    token.ThrowIfCancellationRequested();

                    await memoryStream.WriteAsync(buffer[..bytesLength], token);
                    totalBytesLength += bytesLength;
                }
                
                token.ThrowIfCancellationRequested();

                if (memoryStream.TryGetBuffer(out var segment) ) {
                    // TODO. handler 개발 후 처리
                }

                await memoryStream.DisposeAsync();
            }
        }
    }

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        // TODO. handler 개발 후 처리
        

        return true;
    }
}

public interface IJsonData {
    
}