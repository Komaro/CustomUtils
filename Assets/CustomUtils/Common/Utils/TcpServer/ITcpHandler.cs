using System.Threading;
using System.Threading.Tasks;

public interface ITcpHandler { }

public interface ITcpReceiveHandler : ITcpHandler {
    
    public Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
}

public interface ITcpSendHandler<in TData> : ITcpHandler where TData : ITcpPacket {
    
    public Task SendAsync(TcpSession session, TData send, CancellationToken token);
}