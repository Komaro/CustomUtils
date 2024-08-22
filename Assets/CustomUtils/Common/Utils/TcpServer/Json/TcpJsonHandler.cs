using System;
using System.Threading;
using System.Threading.Tasks;

[SingletonParam(typeof(TcpJsonHandler<>))]
public class TcpJsonHandlerProvider : SingletonWithParameter<TcpJsonHandlerProvider, TcpHandlerProvider<TCP_BODY>> { }

public abstract class TcpJsonHandler<TData> : TcpHandler<TData, TcpHeader> where TData : TcpJsonPacket, ITcpPacket {

    public override TcpHeader CreateHeader(TcpSession session, int length) => new(session.ID, Body, length);

    public override async Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (bytes.GetString().TryToJson<TData>(out var data)) {
            return await ReceiveDataAsync(session, data, token);
        }
        
        throw new InvalidCastException();
    }

    public override async Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (session.Connected && bytes.GetString().TryToJson<TData>(out var data)) {
            return await SendDataAsync(session, data, token);
        }

        throw new InvalidCastException();
    }
    
    public override async Task<bool> SendDataAsync(TcpSession session, TData data, CancellationToken token) {
        if (session.Connected == false) {
            throw new DisconnectSessionException(session);
        }
    
        if (VerifyData(session, data)) {
           var bytes = data.ToBytes();
           var header = CreateHeader(session, bytes.Length);
           await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
           await WriteAsyncWithCancellationCheck(session, bytes, token);
           return true;
        }

        Logger.TraceError($"Invalid Data || {data.GetType().Name}");
        return false;
    }

    protected override bool VerifyData(TcpSession session, TData data) => data.IsValid() && session.VerifySession(data.sessionId);
}

[TcpHandler(TCP_BODY.CONNECT)]
public class JsonConnectSession : TcpJsonHandler<TcpJsonSessionConnect> {

    public override async Task<TcpJsonSessionConnect> ReceiveDataAsync(TcpSession session, TcpJsonSessionConnect data, CancellationToken token) {
        await Task.CompletedTask;
        Logger.TraceLog($"Receive Session Data || {data.sessionId}");
        return data;
    }
}

[TcpHandler(TCP_BODY.CONNECT_RESPONSE)]
public class JsonResponseSession : TcpJsonHandler<TcpJsonSessionConnectResponse> {

    public override async Task<TcpJsonSessionConnectResponse> ReceiveDataAsync(TcpSession session, TcpJsonSessionConnectResponse data, CancellationToken token) {
        await Task.CompletedTask;
        return data;
    }
}

[TcpHandler(TCP_BODY.TEST_REQUEST)]
public class JsonRequestTest : TcpJsonHandler<TcpJsonTestRequest> {

    public override async Task<TcpJsonTestRequest> ReceiveDataAsync(TcpSession session, TcpJsonTestRequest data, CancellationToken token) {
        Logger.TraceLog($"Response || {data.sessionId} || {data.requestText}");
        if (session.Connected && session.ID == data.sessionId && TcpJsonHandlerProvider.inst.TryGetSendHandler<TcpJsonTestResponse>(out var handler)) {
            var responseData = new TcpJsonTestResponse {
                sessionId = session.ID,
                responseText = data.requestText,
            };
            
            await handler.SendDataAsync(session, responseData, token);
        }
        
        return data;
    }
}

[TcpHandler(TCP_BODY.TEST_RESPONSE)]
public class JsonResponseTest : TcpJsonHandler<TcpJsonTestResponse> {

    public override async Task<TcpJsonTestResponse> ReceiveDataAsync(TcpSession session, TcpJsonTestResponse data, CancellationToken token) {
        Logger.TraceLog($"Response || {data.sessionId} || {data.responseText}");
        await Task.CompletedTask;
        return data;
    }
}