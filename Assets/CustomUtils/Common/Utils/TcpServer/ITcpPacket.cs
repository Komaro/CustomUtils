using System;
using System.Runtime.InteropServices;

public interface ITcpPacket {

    public bool IsValid();
}

// TODO. TcpHeader 대체 예정
[StructLayout(LayoutKind.Sequential)]
public struct TcpNewHeader : ITcpPacket {

    public uint sessionId;
    public int body;
    public int byteLength;
    public int error;

    public bool IsValid() => true;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpHeader : ITcpPacket {

    public uint sessionId;
    public TCP_BODY bodyType;
    public int byteLength;

    public TCP_ERROR error;

    public TcpHeader(TCP_BODY bodyType, TCP_ERROR error) {
        sessionId = 0;
        this.bodyType = bodyType;
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

    public TcpHeader(uint sessionId, TCP_BODY bodyType, TCP_ERROR error) {
        this.sessionId = sessionId;
        this.bodyType = bodyType;
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

    public T GetBody<T>() where T : struct, Enum => (T)Enum.ToObject(typeof(T), bodyType);

    public bool IsValid() => sessionId > 0;
}