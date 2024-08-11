using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

[RequiresAttributeImplementation(typeof(TcpHandlerAttribute))]
public abstract class TcpJsonHandler<TData> : ITcpReceiveHandler, ITcpSendHandler<TData> where TData : ITcpPacket {

    protected readonly int bodyCode; 
    
    public TcpJsonHandler() {
        if (GetType().TryGetCustomAttribute<TcpHandlerAttribute>(out var attribute)) {
            // TODO. Set bodyCode
        }
    }

    internal virtual TcpHeader CreateHeader(TcpSession session, TCP_BODY bodyType, int bytesLength) => new() {
        sessionId = session.ID,
        byteLength = bytesLength,
        bodyType = bodyType
    };

    public virtual async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var json = bytes.GetString();
        var data = JsonConvert.DeserializeObject<TData>(json);
        if (data != null) {
            // TODO. Progress
        }
    }

    public virtual async Task SendAsync(TcpSession session, TData send, CancellationToken token) {
        var json = JsonConvert.SerializeObject(send);
        if (string.IsNullOrEmpty(json) == false) {
            var bytes = json.ToBytes();
            var header = new TcpHeader(session, TCP_BODY.TEST, bytes.Length);
            
            await session.Stream.WriteAsync(bytes, token);
        }
    }
}

[TcpHandler(TCP_BODY.SESSION_REQUEST)]
public class JsonRequestSession : TcpJsonHandler<TcpJsonRequestSessionPacket> {

    public override Task SendAsync(TcpSession session, TcpJsonRequestSessionPacket send, CancellationToken token) => throw new System.NotImplementedException();
}

[TcpHandler(TCP_BODY.SESSION_RESPONSE)]
public class JsonResponseSession : TcpJsonHandler<TcpJsonResponseSessionPacket> {

    public override Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) => throw new System.NotImplementedException();

    public override Task SendAsync(TcpSession session, TcpJsonResponseSessionPacket send, CancellationToken token) => throw new System.NotImplementedException();
}