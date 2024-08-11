using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public static class TcpExceptionProvider {
    
    public static async Task<Exception> ResponseExceptionAsync<TPacket>(TcpResponseException<TPacket> exception, TcpClient client, CancellationToken token) where TPacket : ITcpPacket {
        if (client.Connected) {
            var packet = exception.GetPacketBytes();
            await client.GetStream().WriteAsync(packet, token);
            return exception;
        }

        return new DisconnectSessionException(client);
    }
    
    public static async Task<Exception> ResponseExceptionAsync<TPacket>(TcpResponseException<TPacket> exception, TcpSession session, CancellationToken token) where TPacket : ITcpPacket {
        if (session.Connected) {
            var packet = exception.GetPacketBytes(session);
            await session.Stream.WriteAsync(packet, token);
            return exception;
        }

        return new DisconnectSessionException(session);
    }
}

public class DisconnectSessionException : Exception {

    private readonly string _address;
    private readonly uint _id;
    
    public DisconnectSessionException(TcpSession session) : this(session.Client, session.ID) { }
    public DisconnectSessionException(TcpClient client, uint id) : this(client) => _id = id;
    public DisconnectSessionException(TcpClient client) => _address = client.GetIpAddress();

    // TODO. Need Test
    public override string ToString() => $"Session Disconnected.\n[{nameof(IPEndPoint.Address)}] {_address}{(_id != 0 ? $"\n[Session] {_id}" : string.Empty)}";
}

public abstract class TcpResponseException<TPacket> : Exception where TPacket : ITcpPacket {

    public abstract TCP_ERROR Error { get; }
    public abstract TCP_BODY Body { get; }

    public TcpResponseException() { }
    public TcpResponseException(string message) : base(message) { }

    public TPacket CreatePacket(TcpSession session) => CreatePacket(session.ID);
    public abstract TPacket CreatePacket(uint sessionId);

    public virtual byte[] GetPacketBytes() => GetPacketBytes(0);
    public virtual byte[] GetPacketBytes(TcpSession session) => GetPacketBytes(session.ID);
    
    public abstract byte[] GetPacketBytes(uint sessionId);
}

public abstract class TcpStructResponseException : TcpResponseException<TcpHeader> {
    
    public TcpStructResponseException() { }
    public TcpStructResponseException(string message) : base(message) { }
    
    public override TcpHeader CreatePacket(uint sessionId) => new(sessionId, Body, Error);
    
    public override byte[] GetPacketBytes(uint sessionId) {
        var packet = CreatePacket(sessionId);
        return packet.ToBytes();
    }
}

public class InvalidTestCount : TcpStructResponseException {
    
    public override TCP_ERROR Error => TCP_ERROR.INVALID_TEST_COUNT;
    public override TCP_BODY Body => TCP_BODY.TEST;
}

public class InvalidSessionData : TcpStructResponseException {

    public InvalidSessionData(TcpRequestConnect connect) : base($"The value {connect.sessionId} of {nameof(connect.sessionId)} is invalid") { }
    
    public override TCP_ERROR Error => TCP_ERROR.INVALID_SESSION_DATA;
    public override TCP_BODY Body => TCP_BODY.SESSION_REQUEST;
}

public class DuplicateSessionException : TcpStructResponseException {

    public override TCP_ERROR Error => TCP_ERROR.DUPLICATE_SESSION;
    public override TCP_BODY Body => TCP_BODY.SESSION_REQUEST;
}