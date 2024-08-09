using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public interface ITcpStructHandler { }

public interface ITcpStructReceiveHandler : ITcpStructHandler {
    
    public Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
}

public interface ITcpStructSendHandler<in T> : ITcpStructHandler where T : ITcpPacket {
    
    public Task SendAsync(TcpSession session, T send, CancellationToken token);
}

[RequiresAttributeImplementation(typeof(TcpHandlerAttribute))]
public abstract class TcpStructHandler<T> : ITcpStructReceiveHandler, ITcpStructSendHandler<T> where T : struct, ITcpPacket {

    public abstract Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public abstract Task SendAsync(TcpSession session, T send, CancellationToken token);

    internal virtual TcpHeader CreateHeader(TcpSession session, TCP_STRUCT_BODY bodyType, ref T structure) => new() {
        sessionId = session.ID,
        byteLength = Marshal.SizeOf<T>(),
        bodyType = bodyType
    };

    protected async Task WriteAsyncWithCancellationCheck(NetworkStream stream, byte[] bytes, CancellationToken token) {
        await stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }
    
    protected async Task WriteAsyncWithCancellationCheck(TcpSession session, byte[] bytes, CancellationToken token) {
        await session.Stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }

    protected bool TryGetStruct(byte[] bytes, out T structure) {
        var outStructure = bytes.ToStruct<T>();
        if (outStructure.HasValue) {
            structure = outStructure.Value;
            return true;
        }

        structure = default;
        return false;
    }

    public ITcpStructReceiveHandler GetReceiveHandler() => this;
}

#region [Test Implement]
    
[TcpHandler(TCP_STRUCT_BODY.TEST)]
public class RequestTestHandler : TcpStructHandler<TcpRequestTest> {

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (TryGetStruct(bytes, out var request) && request.IsValid()) {
            if (request.count <= 0) {
                throw await TcpStructHandlerProvider.AsyncResponseException(new InvalidTestCount(), session, token);
            }

            if (TcpStructHandlerProvider.TryGetSendHandler<TcpResponseTest>(out var handler)) {
                await handler.SendAsync(session, new TcpResponseTest($"Count : {request.count}"), token);
            }
        }
    }

    public override async Task SendAsync(TcpSession session, TcpRequestTest send, CancellationToken token) {
        if (session.Connected && send.IsValid()) {
            var header = CreateHeader(session, TCP_STRUCT_BODY.TEST, ref send);
            await session.Stream.WriteAsync(header.ToBytes(), token);
            await session.Stream.WriteAsync(send.ToBytes(), token);
        }
    }
}

[TcpHandler(TCP_STRUCT_BODY.STRING)]
public class ResponseTestHandler : TcpStructHandler<TcpResponseTest> {

    internal override TcpHeader CreateHeader(TcpSession session, TCP_STRUCT_BODY bodyType, ref TcpResponseTest structure) => new(session, bodyType, structure.text.GetByteCount());

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var text = bytes.GetString();
        Logger.TraceLog(text);
        await Task.CompletedTask;
    }

    public override async Task SendAsync(TcpSession session, TcpResponseTest send, CancellationToken token) {
        if (session.Connected && send.IsValid()) {
            var header = CreateHeader(session, TCP_STRUCT_BODY.STRING, ref send);
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, send.text.ToBytes(), token);
        }
    }
}

[TcpHandler(TCP_STRUCT_BODY.TEST_TEXT)]
public class ResponseTestTextHandler : TcpStructHandler<TcpResponseTestText> {

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (session.Connected && TryGetStruct(bytes, out var data)) {
            Logger.TraceLog($"{data.text.Length} || {data.text}");
        }

        await Task.CompletedTask;
    }

    public override async Task SendAsync(TcpSession session, TcpResponseTestText send, CancellationToken token) {
        if (session.Connected && send.IsValid()) {
            var header = CreateHeader(session, TCP_STRUCT_BODY.TEST_TEXT, ref send);
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, send.ToBytes(), token);
        }
    }
}

#endregion