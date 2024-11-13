using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[SingletonParam(typeof(TcpStructHandler<>))]
public class TcpStructHandlerProvider : SingletonWithParameter<TcpStructHandlerProvider, TcpHandlerProvider<TCP_BODY>> { }

public abstract class TcpStructHandler<TData> : TcpHandler<TData, TcpHeader> where TData : struct, ITcpPacket {

    public override TcpHeader CreateHeader(TcpSession session, int length) => new(session.ID, Body, length);

    public override async Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var data = bytes.ToStruct<TData>();
        if (data.HasValue) {
            return await ReceiveDataAsync(session, data.Value, token);
        }

        throw new InvalidDataException();
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
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, bytes, token);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return false;
        }
    }
    
    protected override bool VerifyData(TcpSession session, TData data) => session.IsValid() && data.IsValid();
}

#region [Test Implement]

[TcpHandler(TCP_BODY.CONNECT)]
public class TcpSessionConnectHandler : TcpStructHandler<TcpStructSessionConnect> {

    public override async Task<TcpStructSessionConnect> ReceiveDataAsync(TcpSession session, TcpStructSessionConnect data, CancellationToken token) {
        await Task.CompletedTask;
        Logger.TraceLog($"Receive Session Data || {data.sessionId}");
        return data;
    }
}

[TcpHandler(TCP_BODY.CONNECT_RESPONSE)]
public class TcpSessionConnectResponseHandler : TcpStructHandler<TcpStructSessionConnectResponse> {

    public override async Task<TcpStructSessionConnectResponse> ReceiveDataAsync(TcpSession session, TcpStructSessionConnectResponse data, CancellationToken token) {
        await Task.CompletedTask;
        return data;
    }
}

[TcpHandler(TCP_BODY.TEST_REQUEST)]
public class RequestTestHandler : TcpStructHandler<TcpStructTestRequest> {

    public override async Task<TcpStructTestRequest> ReceiveDataAsync(TcpSession session, TcpStructTestRequest data, CancellationToken token) {
        if (data.IsValid() == false) {
            throw new InvalidDataException();
        }

        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpStructTextResponse>(out var handler)) {
            await handler.SendDataAsync(session, new TcpStructTextResponse($"Count : {data.count}"), token);
        }

        return data;
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TcpStructTestRequest data, CancellationToken token) {
        if (session.Connected && data.IsValid()) {
            var bytes = data.ToBytes();
            var header = CreateHeader(session, bytes.Length);
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, bytes, token);
            return true;
        }

        return false;
    }
}

public abstract class TcpStructStringHandler<TData> : TcpStructHandler<TData> where TData : struct, ITcpPacket {

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) => await ReceiveStringAsync(session, bytes.GetString(), token);

    public override async Task<TData> ReceiveDataAsync(TcpSession session, TData data, CancellationToken token) {
        await Task.CompletedTask;
        return data;
    }

    protected abstract Task ReceiveStringAsync(TcpSession session, string text, CancellationToken token);
}

[TcpHandler(TCP_BODY.STRING)]
public class ResponseTestHandler : TcpStructStringHandler<TcpStructTextResponse> {

    protected override async Task ReceiveStringAsync(TcpSession session, string text, CancellationToken token) {
        Logger.TraceLog(text);
        await Task.CompletedTask;
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TcpStructTextResponse data, CancellationToken token) {
        if (session.Connected && VerifyData(session, data)) {
            var bytes = data.text.ToBytes();
            var header = CreateHeader(session, bytes.Length);
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, bytes, token);
            return true;
        }
        
        return false;
    }
}

#endregion