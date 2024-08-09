using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TcpHeader : ITcpPacket {

    public uint sessionId;
    public TCP_STRUCT_BODY bodyType;
    public int byteLength;

    public TCP_ERROR error;

    public TcpHeader(TCP_STRUCT_BODY bodyType, TCP_ERROR error) {
        sessionId = 0;
        this.bodyType = bodyType;
        byteLength = 0;
        this.error = error;
    }
    
    public TcpHeader(TcpSession session) {
        sessionId = session.ID;
        bodyType = TCP_STRUCT_BODY.NONE;
        byteLength = 0;
        error = TCP_ERROR.NONE;
    }
    
    public TcpHeader(uint sessionId, TCP_ERROR error) {
        this.sessionId = sessionId;
        bodyType = TCP_STRUCT_BODY.NONE;
        byteLength = 0; 
        this.error = error;
    }

    public TcpHeader(uint sessionId, TCP_STRUCT_BODY bodyType, TCP_ERROR error) {
        this.sessionId = sessionId;
        this.bodyType = bodyType;
        byteLength = 0;
        this.error = error;
    }
    
    public TcpHeader(TcpSession session, TCP_STRUCT_BODY bodyType) {
        sessionId = session.ID;
        this.bodyType = bodyType;
        byteLength = 0;
        error = TCP_ERROR.NONE;
    }
    
    public TcpHeader(TcpSession session, TCP_STRUCT_BODY bodyType, int byteLength) {
        sessionId = session.ID;
        this.bodyType = bodyType;
        this.byteLength = byteLength;
        error = TCP_ERROR.NONE;
    }

    public bool IsValid() => sessionId > 0;
}

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
public struct TcpError : ITcpPacket {

    public TCP_ERROR error;
    
    public TcpError(TCP_ERROR error) => this.error = error;
    
    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TcpRequestTest : ITcpPacket {

    public readonly int count;

    public TcpRequestTest(int count) => this.count = count;
    public bool IsValid() => count > 0;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TcpResponseTest : ITcpPacket {

    public readonly TCP_ERROR error;
    public readonly string text;

    public TcpResponseTest(TCP_ERROR error) {
        this.error = error;
        text = string.Empty;
    }

    public TcpResponseTest(string text) {
        error = TCP_ERROR.NONE;
        this.text = text;
    }

    public bool IsValid() => error == TCP_ERROR.NONE;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TcpResponseTestText : ITcpPacket {
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
    public readonly string text;

    public TcpResponseTestText(string text) => this.text = text;

    public bool IsValid() => string.IsNullOrEmpty(text) == false;
}

public enum TCP_STRUCT_BODY {
    NONE = 0,
    ERROR = 1,
    HEADER,
    
    // Struct
    CONNECT = 100,
    SESSION,
    TEST,
    TEST_TEXT,
    
    // Bytes
    STRING = 90000,
}
