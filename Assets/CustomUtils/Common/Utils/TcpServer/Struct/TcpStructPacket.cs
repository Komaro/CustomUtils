using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TcpRequestConnect : ITcpPacket {

    public uint sessionId;

    public TcpRequestConnect(TcpSession session) => sessionId = session.ID;
    public TcpRequestConnect(uint sessionId) => this.sessionId = sessionId;
    public bool IsValid() => sessionId != 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpResponseConnect : ITcpPacket {

    public bool isConnected;

    public TcpResponseConnect(bool isConnected) => this.isConnected = isConnected;

    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpErrorPacket : ITcpPacket {

    public TCP_ERROR error;
    
    public TcpErrorPacket(TCP_ERROR error) => this.error = error;
    
    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TcpRequestTest : ITcpPacket {

    public readonly int count;

    public TcpRequestTest(int count) => this.count = count;
    public bool IsValid() => count > 0;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TcpResponseText : ITcpPacket {

    public readonly TCP_ERROR error;
    public readonly string text;

    public TcpResponseText(TCP_ERROR error) {
        this.error = error;
        text = string.Empty;
    }

    public TcpResponseText(string text) {
        error = TCP_ERROR.NONE;
        this.text = text;
    }

    public bool IsValid() => error == TCP_ERROR.NONE;
}