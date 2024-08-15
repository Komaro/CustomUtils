using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[SingletonParam(typeof(TcpStructHandler<>))]
public class TcpStructHandlerProvider : SingletonWithParameter<TcpStructHandlerProvider, TcpHandlerProvider<TCP_BODY>> { }

public abstract class TcpStructHandler<TData> : TcpHandler<TData, TcpHeader> where TData : struct, ITcpPacket {

    public override TcpHeader CreateHeader(TcpSession session, int length) => new() {
        sessionId = session.ID,
        body = Body,
        length = length,
    };

    public override async Task<byte[]> ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        await ReceiveBytesAsync(session, bytes, token);
        return bytes;
    }

    public override async Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var data = bytes.ToStruct<TData>();
        if (data.HasValue) {
            await ReceiveDataAsync(session, data.Value, token);
            return data.Value;
        }

        throw new InvalidDataException();
    }

    public override async Task<bool> SendAsync(TcpSession session, byte[] bytes, CancellationToken token) => await SendBytesAsync(session, bytes, token);

    public override async Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var data = bytes.ToStruct<TData>();
        if (data.HasValue) {
            return await SendDataAsync(session, data.Value, token);
        }

        throw new InvalidCastException();
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TData data, CancellationToken token) {
        if (session.Connected == false) {
            throw new DisconnectSessionException(session);
        }

        if (VerifyData(session, data)) {
            throw new InvalidDataException();
        }
        
        var bytes = data.ToBytes();
        var header = CreateHeader(session, bytes.Length);
        await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
        await WriteAsyncWithCancellationCheck(session, bytes, token);
        return true;
    }
    
    protected override bool VerifyData(TcpSession session, TData data) => session.IsValid() && data.IsValid();
}

#region [Test Implement]
    
[TcpHandler(TCP_BODY.TEST_REQUEST)]
public class RequestTestHandler : TcpStructHandler<TcpRequestTest> {

    public override async Task<TcpRequestTest> ReceiveDataAsync(TcpSession session, TcpRequestTest data, CancellationToken token) {
        if (data.IsValid() == false) {
            throw new InvalidDataException();
        }

        if (TcpStructHandlerProvider.inst.TryGetSendHandler<TcpResponseText>(out var handler)) {
            await handler.SendDataAsync(session, new TcpResponseText($"Count : {data.count}"), token);
        }

        return data;
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TcpRequestTest data, CancellationToken token) {
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

    public override async Task<byte[]> ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        await ReceiveStringAsync(session, bytes.GetString(), token);
        return bytes;
    }

    public override async Task<TData> ReceiveDataAsync(TcpSession session, TData data, CancellationToken token) {
        await Task.CompletedTask;
        return data;
    }

    protected abstract Task ReceiveStringAsync(TcpSession session, string text, CancellationToken token);
}

[TcpHandler(TCP_BODY.STRING)]
public class ResponseTestHandler : TcpStructStringHandler<TcpResponseText> {

    protected override async Task ReceiveStringAsync(TcpSession session, string text, CancellationToken token) {
        Logger.TraceLog(text);
        await Task.CompletedTask;
    }

    public override async Task<bool> SendDataAsync(TcpSession session, TcpResponseText data, CancellationToken token) {
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