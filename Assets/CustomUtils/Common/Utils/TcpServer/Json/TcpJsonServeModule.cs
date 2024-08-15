using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class TcpJsonServeModule : TcpServeModule<TcpHeader, TcpJsonPacket> {

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        var bytes = await ReceiveBytesAsync(client, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue == false) {
            throw new InvalidHeaderException();
        }
        
        bytes = await ReceiveBytesAsync(client, header.Value.length, token);
        if (bytes.GetString().TryToJson<TcpJsonConnectSessionPacket>(out var data)) {
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

            var response = new TcpJsonResponseSessionPacket {
                sessionId = session.ID,
                isActive = true,
            };

            await SendAsync(session, response, token);
            return session;
        }
        
        throw new InvalidDataException();
    }

    public override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        var bytes = await ReceiveBytesAsync(session, TCP_HEADER_SIZE, token);
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
            var bytes = await ReceiveBytesAsync(session, header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        }

        throw new NotImplementHandlerException<TCP_BODY>(header.body);
    }

    public override async Task<bool> SendAsync<TData>(TcpSession session, TData data, CancellationToken token) {
        if (data.IsValid() && TcpJsonHandlerProvider.inst.TryGetSendHandler<TData>(out var handler)) {
            await handler.SendDataAsync(session, data, token);
            return true;
        }
        
        return false;
    }
}
