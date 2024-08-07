using System.Runtime.InteropServices;

public interface ITcpStructure {
    public bool IsValid();
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpHeader : ITcpStructure {

    public uint sessionId;
    public TCP_BODY bodyType;
    public int byteLength;

    public TCP_ERROR error;

    public TcpHeader(TCP_ERROR error) {
        sessionId = 0;
        bodyType = TCP_BODY.NONE;
        byteLength = 0;
        this.error = error;
    }
    
    public TcpHeader(TcpSession session) {
        sessionId = session.ID;
        bodyType = TCP_BODY.NONE;
        byteLength = 0;
        error = TCP_ERROR.NONE;
    }
    
    public TcpHeader(uint sessionId, TCP_ERROR error) {
        this.sessionId = sessionId;
        bodyType = TCP_BODY.NONE;
        byteLength = 0; 
        this.error = error;
    }
    
    public TcpHeader(TcpSession session, TCP_BODY bodyType) {
        sessionId = session.ID;
        this.bodyType = bodyType;
        byteLength = 0;
        error = TCP_ERROR.NONE;
    }
    
    public TcpHeader(TcpSession session, TCP_BODY bodyType, int byteLength) {
        sessionId = session.ID;
        this.bodyType = bodyType;
        this.byteLength = byteLength;
        error = TCP_ERROR.NONE;
    }

    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpRequestConnect : ITcpStructure {

    public uint sessionId;

    public TcpRequestConnect(TcpSession session) => sessionId = session.ID;
    public TcpRequestConnect(uint sessionId) => this.sessionId = sessionId;
    public bool IsValid() => sessionId != 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpResponseConnect : ITcpStructure {

    public bool isConnected;

    public TcpResponseConnect(bool isConnected) => this.isConnected = isConnected;

    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpError : ITcpStructure {

    public TCP_ERROR error;
    
    public TcpError(TCP_ERROR error) => this.error = error;
    
    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpRequestTest : ITcpStructure {

    public readonly int count;

    public TcpRequestTest(int count) => this.count = count;
    public bool IsValid() => count > 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpResponseTest : ITcpStructure {

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

public enum TCP_ERROR {
    NONE = 0,
    
    // Session
    DUPLICATE_SESSION = 100,
    INVALID_SESSION_DATA = 101,
    
    // Data
    MISSING_DATA = 200,
    
    // Progress
    EXCEPTION_PROGRESS = 300,
    
    INVALID_TEST_COUNT = 1000,
}

public enum TCP_BODY {
    NONE = 0,
    ERROR = 1,
    HEADER,
    
    // Struct
    CONNECT = 100,
    SESSION,
    TEST,
    
    // Bytes
    TEST_STRING = 90000,
}
