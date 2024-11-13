using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface ITcpClient {

    public Task Start();
    public Task Start(CancellationToken token);
    public void Close();
    public Task<bool> ConnectAsync(TcpClient client);
}