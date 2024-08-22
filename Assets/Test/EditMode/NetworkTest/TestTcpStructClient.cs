using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

internal class TestTcpStructClient : SimpleTcpClient<TcpHeader, ITcpPacket>, ITestHandler {

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    protected override int HEADER_SIZE => TCP_HEADER_SIZE;

    public TestTcpStructClient(string host, int port) : base(host, port) { }

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpStructSessionConnect>(out var handler)) {
            var newSession = new TcpSession(client, CreateSessionId());
            var sessionData = new TcpStructSessionConnect(newSession);
            if (await handler.SendDataAsync(newSession, sessionData, token) == false) {
                throw new SessionConnectFail(newSession);
            }

            var header = await ReceiveHeaderAsync(newSession, token);
            if (header.IsValid() == false) {
                throw new InvalidHeaderException(header);
            }

            var responseData = await ReceiveDataAsync<TcpStructSessionConnectResponse>(newSession, header, token);
            if (responseData.IsValid() == false) {
                throw new InvalidSessionData();
            }

            if (responseData.isConnected) {
                return newSession;
            }

            throw new SessionConnectFail($"Failed to connect to the session. || {host} || {port}");
        }

        throw new NotImplementHandlerException<TCP_BODY>(typeof(TcpStructSessionConnect));
    }

    public override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        var bytes = await ReadBytesAsync(session, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue) {
            if (header.Value.IsValid() == false) {
                throw new InvalidHeaderException(header);
            }

            return header.Value;
        }

        throw new InvalidHeaderException();
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        } else {
            throw new NotImplementHandlerException<TCP_BODY>(header.body);
        }
    }

    public override async Task<T> ReceiveDataAsync<T>(TcpSession session, TcpHeader header, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetReceiveHandler<T>(out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            return await handler.ReceiveBytesAsync(session, bytes, token);
        }

        throw new NotImplementHandlerException<TCP_BODY>(header.body);
    }

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (data.IsValid() && TcpStructHandlerProvider.inst.TryGetSendHandler<T>(out var handler)) {
            return await handler.SendDataAsync(session, data, token);
        }

        throw new NotImplementHandlerException<TCP_BODY>(data);
    }

    // Temp
    // public override bool Send<T>(uint sessionId, T data) => session != null && session.VerifySession(sessionId) && SendAsync(session, data, cancelToken.Token).Result;
    //
    // // Temp
    // public override async Task<bool> SendAsync<T>(uint sessionId, T data) => session != null && session.VerifySession(sessionId) && await SendAsync(session, data, cancelToken.Token);

    protected override uint CreateSessionId() => (uint) new Random(DateTime.Now.GetHashCode()).Next();

    public void StartTest(CancellationToken token) => Send(session, new TcpStructTestRequest(20));
}