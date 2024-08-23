using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Random = System.Random;

public abstract record TcpStructWrapperBasePacket : ITcpPacket {

    public abstract byte[] ToBytes();
    public abstract bool ToPopulate(byte[] bytes);
    public abstract bool IsValid();
    public abstract Type GetGenericType();
}

public record TcpStructWrapperPacket<TData> : TcpStructWrapperBasePacket where TData : struct, ITcpPacket {

    public TData structure;

    public TcpStructWrapperPacket() { }
    public TcpStructWrapperPacket(ref TData data) => structure = data;

    public TcpStructWrapperPacket(byte[] bytes) {
        var data = bytes.ToStruct<TData>();
        if (data.HasValue) {
            structure = data.Value;
        }
    }

    public override byte[] ToBytes() => structure.ToBytes();

    public override bool ToPopulate(byte[] bytes) {
        var data = bytes.ToStruct<TData>();
        if (data.HasValue) {
            structure = data.Value;
            return true;
        }

        return false;
    }
    
    public override bool IsValid() => structure.IsValid();
    public override Type GetGenericType() => typeof(TData);
}

internal class TestTcpStructWrapperClient : SimpleTcpClient<TcpHeader, TcpStructWrapperBasePacket>, ITestHandler {
    
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    protected override int HEADER_SIZE => TCP_HEADER_SIZE;
    
    public TestTcpStructWrapperClient(string host, int port) : base(host, port) { }

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpStructSessionConnect>(out var handler)) {
            var newSession = new TcpSession(client, CreateSessionId());
            if (await handler.SendDataAsync(newSession, new TcpStructSessionConnect(newSession), token) == false) {
                throw new SessionConnectFail();
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
        var bytes = await ReadBytesAsync(session, HEADER_SIZE, token);
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
        
        throw new NotImplementHandlerException(typeof(T));
    }

    protected override async Task<bool> SendDataAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetHandler(data.GetGenericType(), out var handler)) {
            await handler.SendAsync(session, data.ToBytes(), token);
        }

        throw new NotImplementHandlerException(data);
    }

    protected override ITcpHandler GetHandler(TcpStructWrapperBasePacket data) => TcpStructHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler) ? handler : null;
    protected override byte[] GetBytes(TcpStructWrapperBasePacket data) => data.ToBytes();

    protected override uint CreateSessionId() => (uint) new Random(DateTime.Now.GetHashCode()).Next();

    public void StartTest(CancellationToken token) {
        Logger.TraceLog("Start Test");
        var structure = new TcpStructTestRequest(4533);
        SendDataPublish(session, new TcpStructWrapperPacket<TcpStructTestRequest>(ref structure));
    }
}