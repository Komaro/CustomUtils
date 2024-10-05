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

    public DisconnectSessionException() { }
    public DisconnectSessionException(TcpSession session) : this(session.Client, session.ID) { }
    public DisconnectSessionException(TcpClient client, uint id) : this(client) => _id = id;
    public DisconnectSessionException(TcpClient client) => _address = client.GetIpAddress();

    public override string ToString() => $"Session Disconnected.\n[{nameof(IPEndPoint.Address)}] {_address}{(_id != 0 ? $"\n[Session] {_id}" : string.Empty)}";
}

public class SessionConnectFail : Exception {

    public SessionConnectFail() : base("Failed to connect to the session") { }
    public SessionConnectFail(TcpSession session) : base($"Failed to connect to the {nameof(TcpSession)} || {session.ID}") { }
    public SessionConnectFail(string message) : base(message) { }
}

public class InvalidHeaderException : Exception {

    private const string INVALID_RECEIVED_MESSAGE = "Invalid type header received";
    
    public InvalidHeaderException() : base(INVALID_RECEIVED_MESSAGE) { }
    public InvalidHeaderException(string message) : base($"{INVALID_RECEIVED_MESSAGE} || {message}") { }
    public InvalidHeaderException(ITcpPacket header) : base($"{INVALID_RECEIVED_MESSAGE} || {header.GetType().FullName}") { }
}

public class NotImplementHandlerException : Exception {

    public NotImplementHandlerException(Type type) : base($"There is no handler implemented for the '{type.FullName}'") { }
    public NotImplementHandlerException(object ob) : base($"There is no handler implemented for the '{ob.GetType().FullName}'") { }
    public NotImplementHandlerException(Enum type) : base($"There is no handler implemented for the '{type}'") { }
}

public class NotImplementHandlerException<TEnum> : Exception where TEnum : struct, Enum {

    public NotImplementHandlerException(Type type) : base($"There is no handler implemented for the '{type.FullName}'") { }
    public NotImplementHandlerException(object ob) : base($"There is no handler implemented for the '{ob.GetType().FullName}'") { }
    public NotImplementHandlerException(int type) : base($"There is no handler implemented for the '{EnumUtil.ConvertFast<TEnum>(type)}'") { }
    public NotImplementHandlerException(TEnum type) : base($"There is no handler implemented for the '{type}'") { }
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
    
    public override TcpHeader CreatePacket(uint sessionId) => new(sessionId, (int)Error);
    
    public override byte[] GetPacketBytes(uint sessionId) {
        var packet = CreatePacket(sessionId);
        return packet.ToBytes();
    }
}

public class InvalidSessionData : TcpStructResponseException {

    public InvalidSessionData() : base("Invalid session data") { }
    
    public override TCP_ERROR Error => TCP_ERROR.INVALID_SESSION_DATA;
    public override TCP_BODY Body => TCP_BODY.CONNECT_RESPONSE;
}

public class DuplicateSessionException : TcpStructResponseException {

    public override TCP_ERROR Error => TCP_ERROR.DUPLICATE_SESSION;
    public override TCP_BODY Body => TCP_BODY.CONNECT_RESPONSE;
}