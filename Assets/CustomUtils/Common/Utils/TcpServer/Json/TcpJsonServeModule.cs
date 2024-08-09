using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TcpJsonServeModule : TcpServeModule<JsonHeader, IJsonData> {

    public override Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) => throw new System.NotImplementedException();

    public override Task<JsonHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) => throw new System.NotImplementedException();

    public override Task ReceiveDataAsync(TcpSession session, JsonHeader header, CancellationToken token) => throw new System.NotImplementedException();

    public override Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) => throw new System.NotImplementedException();
}

public class JsonHeader {
    
}

public interface IJsonData {
    
}