using System;
using System.Threading;
using System.Threading.Tasks;

[SingletonParam(typeof(TcpJsonHandler<>))]
public class TcpJsonHandlerProvider : SingletonWithParameter<TcpJsonHandlerProvider, TcpHandlerProvider<TCP_BODY>> { }

public abstract class TcpJsonHandler<TData> : TcpHandler<TData, TcpHeader> where TData : TcpJsonPacket, ITcpPacket {

    public override TcpHeader CreateHeader(TcpSession session, int length) {
        return new TcpHeader {
            sessionId = session.ID,
            body = Body,
            length = length,
        };
    }

    public override async Task<byte[]> ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        await ReceiveBytesAsync(session, bytes, token);
        return bytes;
    }

    public override async Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (bytes.GetString().TryToJson<TData>(out var data)) {
            await ReceiveDataAsync(session, data, token);
            return data;
        }
        
        throw new InvalidCastException();
    }

    public override async Task<bool> SendAsync(TcpSession session, byte[] bytes, CancellationToken token) => await SendBytesAsync(session, bytes, token);

    public override async Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (session.Connected && bytes.GetString().TryToJson<TData>(out var data)) {
            return await SendDataAsync(session, data, token);
        }

        return false;
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TData data, CancellationToken token) {
        if (VerifyData(session, data)) {
           var bytes = data.ToBytes();
           var header = CreateHeader(session, bytes.Length);
           await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
           await WriteAsyncWithCancellationCheck(session, bytes, token);
           return true;
        }

        return false;
    }

    protected override bool VerifyData(TcpSession session, TData data) => data.IsValid() && session.VerifySession(data.sessionId);
}

[TcpHandler(TCP_BODY.CONNECT)]
public class JsonConnectSession : TcpJsonHandler<TcpJsonConnectSessionPacket> {

    public override async Task<TcpJsonConnectSessionPacket> ReceiveDataAsync(TcpSession session, TcpJsonConnectSessionPacket data, CancellationToken token) {
        await Task.CompletedTask;
        return data;
    }
}

[TcpHandler(TCP_BODY.SESSION_RESPONSE)]
public class JsonResponseSession : TcpJsonHandler<TcpJsonResponseSessionPacket> {

    public override async Task<TcpJsonResponseSessionPacket> ReceiveDataAsync(TcpSession session, TcpJsonResponseSessionPacket data, CancellationToken token) {
        Logger.TraceLog(data.isActive ? "Session Connected" : "Session Connect Failed");
        await Task.CompletedTask;
        return data;
    }
}

[TcpHandler(TCP_BODY.TEST_REQUEST)]
public class JsonRequestTest : TcpJsonHandler<TcpJsonRequestTestPacket> {

    public override async Task<TcpJsonRequestTestPacket> ReceiveDataAsync(TcpSession session, TcpJsonRequestTestPacket data, CancellationToken token) {
        Logger.TraceLog($"Response || {data.sessionId} || {data.requestText}");
        if (session.Connected && session.ID == data.sessionId && TcpJsonHandlerProvider.inst.TryGetSendHandler<TcpJsonResponseTestPacket>(out var handler)) {
            var responseData = new TcpJsonResponseTestPacket {
                sessionId = session.ID,
                responseText = data.requestText,
            };
            
            await handler.SendDataAsync(session, responseData, token);
        }
        
        return data;
    }
}

[TcpHandler(TCP_BODY.TEST_RESPONSE)]
public class JsonResponseTest : TcpJsonHandler<TcpJsonResponseTestPacket> {

    public override async Task<TcpJsonResponseTestPacket> ReceiveDataAsync(TcpSession session, TcpJsonResponseTestPacket data, CancellationToken token) {
        Logger.TraceLog($"Response || {data.sessionId} || {data.responseText}");
        await Task.CompletedTask;
        return data;
    }
}