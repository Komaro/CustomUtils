using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TcpStructSessionConnect : ITcpPacket {

    public uint sessionId;

    public TcpStructSessionConnect(TcpSession session) => sessionId = session.ID;
    public TcpStructSessionConnect(uint sessionId) => this.sessionId = sessionId;
    public bool IsValid() => sessionId != 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpStructSessionConnectResponse : ITcpPacket {

    public bool isConnected;

    public TcpStructSessionConnectResponse(bool isConnected) => this.isConnected = isConnected;

    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpStructTestRequest : ITcpPacket {

    public int count;

    public TcpStructTestRequest(int count) => this.count = count;
    public bool IsValid() => count > 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpStructTextResponse : ITcpPacket {

    public TCP_ERROR error;
    public string text;

    public TcpStructTextResponse(TCP_ERROR error) {
        this.error = error;
        text = string.Empty;
    }

    public TcpStructTextResponse(string text) {
        error = TCP_ERROR.NONE;
        this.text = text;
    }

    public bool IsValid() => error == TCP_ERROR.NONE;
}