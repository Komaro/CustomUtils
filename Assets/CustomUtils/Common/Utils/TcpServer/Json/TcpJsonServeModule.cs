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
    
        var bytes = await ReadBytesAsync(client, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue == false) {
            throw new InvalidHeaderException();
        }
        
        bytes = await ReadBytesAsync(client, header.Value.length, token);
        if (bytes.GetString().TryToJson<TcpJsonSessionConnect>(out var data)) {
            if (data.IsValid() == false) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new InvalidSessionData(), client, token);
            }
            
            if (sessionDic.TryGetValue(data.sessionId, out var session) && session.Connected) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new DuplicateSessionException(), session, token);
            }
            
            session = new TcpSession(client, data.sessionId);
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
        var bytes = await ReadBytesAsync(session, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue) {
            return header.Value;
        }

        throw new InvalidHeaderException();
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        if (header.IsValid()) {
            throw new InvalidHeaderException(header.ToStringAllFields());
        }
    
        if (TcpJsonHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        }

        throw new NotImplementHandlerException<TCP_BODY>(header.body);
    }

    public override async Task<bool> SendAsync<TData>(TcpSession session, TData data, CancellationToken token) {
        if (data.IsValid() && TcpJsonHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler)) {
            await handler.SendAsync(session, data.ToBytes(), token);
            return true;
        }
        
        return false;
    }
}
