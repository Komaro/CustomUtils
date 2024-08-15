using System;
using System.Runtime.InteropServices;

public interface ITcpPacket {

    public bool IsValid();
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpPacket : ITcpPacket {
    
    public ITcpPacket header;
    public ITcpPacket body;

    public bool IsValid() => (header?.IsValid() ?? false) && (body?.IsValid() ?? false);
}

// TODO. TcpHeader 대체 예정
[StructLayout(LayoutKind.Sequential)]
public struct TcpHeader : ITcpPacket {

    public uint sessionId;
    public int body;
    public int length;
    public int error;

    public TcpHeader(uint sessionId, int body, int length) {
        this.sessionId = sessionId;
        this.body = body;
        this.length = length;
        error = 0;
    }

    public TcpHeader(uint sessionId, int error) {
        this.sessionId = sessionId;
        body = 0;
        length = 0;
        this.error = error;
    }
    
    public bool TryGetEnumBody<TEnum>(out TEnum enumValue) where TEnum : struct, Enum => EnumUtil.TryConvertFast(body, out enumValue);
    public TEnum GetEnumBody<TEnum>() where TEnum : struct, Enum => EnumUtil.ConvertFast<TEnum>(body);

    public bool IsValid() => true;
}

public enum TCP_BODY {
    NONE = 0,
    ERROR = 1,
    HEADER,
    
    CONNECT = 100,
    
    SESSION_RESPONSE,
    
    TEST_REQUEST,
    TEST_RESPONSE,
    
    // Binary
    STRING = 90000,
}
