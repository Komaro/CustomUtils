using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ping = System.Net.NetworkInformation.Ping;

public class TcpPingServeModule : TcpServeModule<bool, Ping> {

    protected override async Task ServeAsync(CancellationToken token) {
        try {
            while (token.IsCancellationRequested == false) {
                try {
                    var client = await AcceptAsync(token);
                    token.ThrowIfCancellationRequested();
                    _ = Task.Run(() => SendAsync(new TcpSession(client, 9999u), new Ping(), token), token);
                    await connectChannel.Writer.WriteAsync((client, token), channelCancelToken.Token);
                } catch (SocketException ex) {
                    Logger.TraceLog(ex);
                } catch (SystemException ex) {
                    Logger.TraceLog(ex);
                } 
            }
        } catch (OperationCanceledException) { }
    }

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        await Task.CompletedTask;
        return new TcpSession(client, 99995u);
    }

    public override async Task<bool> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        using (var owner = memoryPool.Rent(sizeof(bool))) {
            var buffer = owner.Memory;
            var bytesLength = await session.Stream.ReadAsync(buffer, token);
            if (bytesLength == 0) {
                throw new DisconnectSessionException(session);
            }
            
            token.ThrowIfCancellationRequested();
            var header = MemoryMarshal.Read<bool>(buffer.Span);
            return header;
        }
    }

    public override async Task ReceiveDataAsync(TcpSession session, bool header, CancellationToken token) {
        Logger.TraceLog(header);
        await Task.CompletedTask;
    }

    public override async Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) {
        while (token.IsCancellationRequested == false) {
            var buffer = "test".ToBytes();
            await session.Stream.WriteAsync(buffer, token);
            await Task.Delay(1000, token);
        }
        
        return true;
    }
}
