using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class TcpStructServeModule : TcpServeModule<TcpHeader, ITcpPacket> {
    
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        var bytes = await ReadBytesAsync(client, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue == false) {
            throw new InvalidHeaderException();
        }
        
        bytes = await ReadBytesAsync(client, header.Value.length, token);
        var data = bytes.ToStruct<TcpStructSessionConnect>();
        if (data.HasValue) {
            if (data.Value.IsValid() == false) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new InvalidSessionData(), client, token);
            }
            
            var id = data.Value.sessionId;
            if (sessionDic.TryGetValue(id, out var session) && session.Connected) {
                throw await TcpExceptionProvider.ResponseExceptionAsync(new DuplicateSessionException(), client, token);
            }
                
            session = new TcpSession(client, id);
            sessionDic.AddOrUpdate(id, session, (_, oldSession) => {
                oldSession.Close();
                return session;
            });

            var response = new TcpStructSessionConnectResponse(true);
            await SendAsync(session, response, token);

            return session;
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
        if (header.IsValid() == false) {
            throw new InvalidHeaderException(header.ToStringAllFields());
        }

        if (TcpStructHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        }

        throw new NotImplementHandlerException<TCP_BODY>(header.body);
    }

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (data.IsValid() && TcpStructHandlerProvider.inst.TryGetSendHandler<T>(out var handler)) {
            await handler.SendDataAsync(session, data, token);
            return true;
        }

        return false;
    }

    // Temp
    public override bool Send<T>(TcpSession session, T data) => SendAsync(session, data, cancelToken.Token).Result;

    // Temp
    public override async Task<bool> SendAsync<T>(TcpSession session, T data) => await SendAsync(session, data, cancelToken.Token);
}