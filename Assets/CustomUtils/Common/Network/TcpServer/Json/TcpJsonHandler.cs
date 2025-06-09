using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

[SingletonParam(typeof(TcpJsonHandler<>))]
public class TcpJsonHandlerProvider : SingletonWithParameter<TcpJsonHandlerProvider, TcpHandlerProvider<TCP_BODY>> { }

public abstract class TcpJsonHandler<TData> : TcpHandler<TData, TcpHeader> where TData : TcpJsonPacket, ITcpPacket {

    protected readonly MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

    public override TcpHeader CreateHeader(TcpSession session, int length) => new(session.ID, Body, length);

    public override async Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (bytes.GetString().TryToJson<TData>(out var data)) {
            return await ReceiveDataAsync(session, data, token);
        }

        throw new InvalidCastException();
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TData data, CancellationToken token) {
        if (session.Connected == false) {
            throw new DisconnectSessionException(session);
        }
    
        if (VerifyData(session, data)) {
            return await SendBytesAsync(session, data.ToBytes(), token);
        }

        Logger.TraceError($"Invalid Data || {data.GetType().Name}");
        return false;
    }

    public override async Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        try {
            var header = CreateHeader(session, bytes.Length);
            using(var owner = memoryPool.Rent(header.length + bytes.Length)) {
                var buffer = owner.Memory;

                // TODO. Copy buffer

                await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
                await WriteAsyncWithCancellationCheck(session, bytes, token);
            }

            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
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
        Logger.TraceLog($"{nameof(JsonRequestTest)} || {data.sessionId} || {data.requestText}");
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
        Logger.TraceLog($"{nameof(JsonResponseTest)} || {data.sessionId} || {data.responseText}");
        await Task.CompletedTask;
        return data;
    }
}

[TcpHandler(TCP_BODY.DISCONNECT)]
public class JsonRequestDisconnect : TcpJsonHandler<TcpJsonDisconnectRequest> {

    public override async Task<TcpJsonDisconnectRequest> ReceiveDataAsync(TcpSession session, TcpJsonDisconnectRequest data, CancellationToken token) {
        Logger.TraceLog($"{nameof(JsonRequestDisconnect)} || {data.delaySeconds}");
        if (data.delaySeconds > 0) {
            await Task.Delay(data.delaySeconds * 1000, token);
        }
        
        throw new DisconnectSessionException(session);
    }
}