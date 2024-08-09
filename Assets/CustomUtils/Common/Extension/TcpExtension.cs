using System.Net;
using System.Net.Sockets;

public static class TcpExtension {

    public static string GetIpAddress(this TcpClient session) => session.Client != null ? session.Client.GetIpAddress() : string.Empty;
    public static string GetIpAddress(this Socket socket) => socket.RemoteEndPoint is IPEndPoint ipEndPoint ? ipEndPoint.Address.ToString() : string.Empty;
    public static string GetIpAddress(this EndPoint endPoint) => endPoint is IPEndPoint ipEndPoint ? ipEndPoint.Address.ToString() : string.Empty;
}