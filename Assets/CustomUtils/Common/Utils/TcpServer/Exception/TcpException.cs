using System;
using System.Net;
using System.Net.Sockets;

public class DisconnectSessionException : Exception {

    private readonly string _address;
    private readonly uint _id;
    
    public DisconnectSessionException(TcpSession session) : this(session.Client, session.ID) { }
    public DisconnectSessionException(TcpClient client, uint id) : this(client) => _id = id;
    public DisconnectSessionException(TcpClient client) => _address = client.GetIpAddress();

    // TODO. Need Test
    public override string ToString() => $"Session Disconnected.\n[{nameof(IPEndPoint.Address)}] {_address}{(_id != 0 ? $"\n[Session] {_id}" : string.Empty)}";
}

// TODO. 모듈화
public abstract class TcpResponseException<T> : Exception where T : ITcpPacket {

    public TcpResponseException() { }
    public TcpResponseException(string message) : base(message) { }

    public abstract TCP_ERROR Error { get; }
    public abstract TCP_STRUCT_BODY Body { get; }

    public virtual TcpHeader CreateErrorHeader() => new(Body, Error);
    public virtual TcpHeader CreateErrorHeader(uint sessionId) => new(sessionId, Body, Error);
    public virtual TcpHeader CreateErrorHeader(uint sessionId, TCP_STRUCT_BODY bodyType) => new(sessionId, bodyType, Error);
}

public class InvalidSessionData : TcpResponseException<TcpError> {
    
    public InvalidSessionData(TcpRequestConnect connect) : base($"The value {connect.sessionId} of {nameof(connect.sessionId)} is invalid") { }
    public override TCP_ERROR Error => TCP_ERROR.INVALID_SESSION_DATA;
    public override TCP_STRUCT_BODY Body => TCP_STRUCT_BODY.SESSION;
}

public class DuplicateSessionException : TcpResponseException<TcpError> {

    public override TCP_ERROR Error => TCP_ERROR.DUPLICATE_SESSION;
    public override TCP_STRUCT_BODY Body => TCP_STRUCT_BODY.SESSION;
}

public class InvalidTestCount : TcpResponseException<TcpError> {
    
    public override TCP_ERROR Error => TCP_ERROR.INVALID_TEST_COUNT;
    public override TCP_STRUCT_BODY Body => TCP_STRUCT_BODY.TEST;
}



public abstract class _TcpResponseException<TPacket> : Exception where TPacket : ITcpPacket {

    public abstract TCP_ERROR Error { get; }
    public abstract TCP_STRUCT_BODY Body { get; }
    
    

    public TPacket CreatePacket(TcpSession session) => CreatePacket(session.ID);
    public abstract TPacket CreatePacket(uint sessionId);

    public byte[] GetPacketBytes(TcpSession session) => GetPacketBytes(session.ID);
    public abstract byte[] GetPacketBytes(uint sessionId);
}

public abstract class TcpStructResponseException : _TcpResponseException<TcpHeader> {
    
    public override TcpHeader CreatePacket(uint sessionId) => new(Body, Error);
    
    public override byte[] GetPacketBytes(uint sessionId) {
        var packet = CreatePacket(sessionId);
        return packet.ToBytes();
    }
}

public class _InvalidTestCount : TcpStructResponseException {
    
    public override TCP_ERROR Error => TCP_ERROR.INVALID_TEST_COUNT;
    public override TCP_STRUCT_BODY Body => TCP_STRUCT_BODY.TEST;
}
