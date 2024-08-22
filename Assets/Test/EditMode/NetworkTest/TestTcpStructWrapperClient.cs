using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public abstract record TcpStructWrapperBasePacket : ITcpPacket {

    public abstract byte[] ToBytes();
    public abstract bool ToPopulate(byte[] bytes);
    public abstract bool IsValid();
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
}

public record TcpStructWrapperSessionConnect : TcpStructWrapperPacket<TcpStructSessionConnect> {
    
}

public record TcpStructWrapperSessionConnectResponse : TcpStructWrapperPacket<TcpStructSessionConnectResponse> {
    
}

internal class TestTcpStructWrapperClient : SimpleTcpClient<TcpHeader, ITcpPacket>, ITestHandler {

    public TestTcpStructWrapperClient(string host, int port) : base(host, port) { }

    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    protected override int HEADER_SIZE => TCP_HEADER_SIZE;

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpStructSessionConnect>(out var handler)) {
            var newSession = new TcpSession(client, CreateSessionId());
            var sessionData = new TcpStructSessionConnect(newSession);
            
            await handler.SendDataAsync(session, sessionData, token);
            
            var header = await ReceiveHeaderAsync(newSession, token);
            if (header.IsValid() == false) {
                throw new InvalidHeaderException(header);
            }
            
            var responseData = await ReceiveDataAsync<TcpStructWrapperPacket<TcpStructSessionConnectResponse>>(newSession, header, token);
            if (responseData.IsValid() == false) {
                throw new InvalidSessionData();
            }
            
            if (responseData.structure.isConnected) {
                return newSession;
            }

            throw new SessionConnectFail($"Failed to connect to the session. || {host} || {port}");
        }

        throw new NotImplementHandlerException<TCP_BODY>(typeof(TcpStructSessionConnect));
    }

    public override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
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

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (TcpStructHandlerProvider.inst.TryGetSendHandler<T>(out var handler)) {
            await handler.SendDataAsync(session, data, token);
        }

        throw new NotImplementHandlerException(data);
    }

    protected override uint CreateSessionId() => (uint) new Random(DateTime.Now.GetHashCode()).Next();

    public void StartTest(CancellationToken token) {
        Logger.TraceLog("Start Test");
    }
}