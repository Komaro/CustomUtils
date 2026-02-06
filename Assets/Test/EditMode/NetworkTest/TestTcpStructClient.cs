using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// TODO. 정상 테스트 진행되지 않음
internal class TestTcpStructClient : SimpleTcpClient<TcpHeader, ITcpPacket>, ITestHandler {

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    protected override int HEADER_SIZE => TCP_HEADER_SIZE;

    public TestTcpStructClient(string host, int port) : base(host, port) { }

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpStructSessionConnect>(out var handler)) {
            var newSession = new TcpSession(client, CreateSessionId());
            if (await handler.SendDataAsync(newSession, new TcpStructSessionConnect(newSession), token) == false) {
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

    protected override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
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

    protected override async Task<bool> SendDataAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (data.IsValid() && TcpStructHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler)) {
            var bytes = GetBytes(data);
            if (bytes != Array.Empty<byte>()) {
                await handler.SendAsync(session, bytes, token);
                return true;
            }
        }

        throw new NotImplementHandlerException<TCP_BODY>(data);
    }

    protected override ITcpHandler GetHandler(ITcpPacket data) => TcpStructHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler) ? handler : null;

    protected override byte[] GetBytes(ITcpPacket data) {
        var type = data.GetType();
        if (type.IsStruct()) {
            var size = Marshal.SizeOf(type);
            var pointer = Marshal.AllocHGlobal(size);
            var bytes = new byte[size];
        
            try {
                Marshal.StructureToPtr(data, pointer, true);
                Marshal.Copy(pointer, bytes, 0, size);
            } finally {
                Marshal.FreeHGlobal(pointer);
            }
        }

        return Array.Empty<byte>();
    }

    protected override uint CreateSessionId() => (uint) new Random(DateTime.Now.GetHashCode()).Next();

    public void StartTest(CancellationToken token) => SendDataPublish(session, new TcpStructTestRequest(20));
}