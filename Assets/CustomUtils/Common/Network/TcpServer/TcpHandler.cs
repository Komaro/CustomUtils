using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface ITcpHandler {
    
    public Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public Task SendAsync(TcpSession session, byte[] bytes, CancellationToken token);
}

public interface ITcpReceiveHandler<TData> : ITcpHandler {

    public Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public Task<TData> ReceiveDataAsync(TcpSession session, TData data, CancellationToken token);
}

public interface ITcpSendHandler<in TData> : ITcpHandler {

    public Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public Task<bool> SendDataAsync(TcpSession session, TData send, CancellationToken token);
}

[RequiresAttributeImplementation(typeof(TcpHandlerAttribute))]
public abstract class TcpHandler<TData, THeader> : ITcpReceiveHandler<TData>, ITcpSendHandler<TData> where TData : ITcpPacket where THeader : ITcpPacket {

    public int Body { get; }

    protected TcpHandler() {
        if (GetType().TryGetCustomAttribute<TcpHandlerAttribute>(out var attribute)) {
            Body = attribute.body.ToInt32();
        }
    }

    public abstract THeader CreateHeader(TcpSession session, int length);

    public virtual async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) => await ReceiveBytesAsync(session, bytes, token);
    public virtual async Task SendAsync(TcpSession session, byte[] bytes, CancellationToken token) => await SendBytesAsync(session, bytes, token);
    
    public abstract Task<TData> ReceiveBytesAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public abstract Task<TData> ReceiveDataAsync(TcpSession session, TData data, CancellationToken token);
    
    public abstract Task<bool> SendDataAsync(TcpSession session, TData data, CancellationToken token);
    public abstract Task<bool> SendBytesAsync(TcpSession session, byte[] bytes, CancellationToken token);
    
    protected async Task WriteAsyncWithCancellationCheck(NetworkStream stream, byte[] bytes, CancellationToken token) {
        await stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }
    
    protected async Task WriteAsyncWithCancellationCheck(TcpSession session, byte[] bytes, CancellationToken token) {
        await session.Stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }

    protected abstract bool VerifyData(TcpSession session, TData data);
}

public class TcpHandlerAttribute : Attribute {
    
    public readonly Enum body;
    
    public TcpHandlerAttribute(object type) {
        if (type is Enum enumType) {
            body = enumType;
        }
    }
}