using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class TcpJsonServeModule : TcpServeModule<TcpHeader, TcpJsonPacket> {

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    
    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        Logger.TraceLog($"Start {nameof(ConnectSessionAsync)}", Color.LightGreen);

        // TODO. 완성되지 않은 Session 생성 후 전체 처리가 완료되면 완성된 Session을 반환하도록 구현
        var session = new TcpSession(client);
        var bytes = await session.ReadBytesAsync(TCP_HEADER_SIZE, token);
        
        // var bytes = await ReadBytesAsync(client, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue == false) {
            throw new InvalidHeaderException();
        }

        bytes = await session.ReadBytesAsync(header.Value.length, token);
        // bytes = await ReadBytesAsync(client, header.Value.length, token);
        if (bytes.GetString().TryToJson<TcpJsonSessionConnect>(out var data)) {
            if (data.IsValid() == false) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new InvalidSessionData(), client, token);
            }
            
            if (sessionDic.TryGetValue(data.sessionId, out var duplicateSession) && duplicateSession.Connected) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new DuplicateSessionException(), duplicateSession, token);
            }

            session.ID = data.sessionId;
            // session = new TcpSession(client, data.sessionId);
            sessionDic.AddOrUpdate(data.sessionId, session, (_, oldSession) => {
                oldSession.Close();
                return session;
            });

            var response = new TcpJsonSessionConnectResponse {
                sessionId = session.ID,
                isActive = true,
            };

            if (await SendAsync(session, response, token)) {
                Logger.TraceLog($"{nameof(TcpSession)} connection was successful", Color.SkyBlue);
                return session;
            }

            throw new SessionConnectFail(session);
        }
        
        throw new InvalidDataException();
    }

    public override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        /*var bytes = await ReadBytesAsync(session, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue) {
            return header.Value;
        }*/

        var bytes = await session.ReadBytesAsync(TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue) {
            return header.Value;
        }
        
        throw new InvalidHeaderException();
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        if (header.IsValid() == false) {
            throw new InvalidHeaderException(header.ToStringAllFields());
        }

        if (TcpJsonHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
            // var bytes = await ReadBytesAsync(session, header.length, token);
            // await handler.ReceiveAsync(session, bytes, token);

            var bytes = await session.ReadBytesAsync(header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        } else {
            throw new NotImplementHandlerException<TCP_BODY>(header.body);
        }
    }

    public override async Task<bool> SendAsync<TData>(TcpSession session, TData data, CancellationToken token) {
        if (data.IsValid() && TcpJsonHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler)) {
            await handler.SendAsync(session, data.ToBytes(), token);
            return true;
        }
        
        return false;
    }
}
