using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

internal class TestTcpJsonClient : SimpleTcpClient<TcpHeader, TcpJsonPacket>, ITestHandler {
    
    private static readonly int TCP_HEADER_SIZE = Marshal.SizeOf<TcpHeader>();
    protected override int HEADER_SIZE => TCP_HEADER_SIZE;

    public TestTcpJsonClient(string host, int port) : base(host, port) { }

    public override async Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token) {
        var newSession = new TcpSession(client, CreateSessionId());
        if (TcpJsonHandlerProvider.inst.TryGetSendHandler<TcpJsonSessionConnect>(out var handler)) {
            if (await handler.SendDataAsync(newSession, new TcpJsonSessionConnect(newSession.ID), token) == false) {
                throw new SessionConnectFail();
            }
            
            var header = await ReceiveHeaderAsync(newSession, token);
            if (header.IsValid() == false) {
                throw new InvalidHeaderException(header);
            }

            var responseData = await ReceiveDataAsync<TcpJsonSessionConnectResponse>(newSession, header, token);
            if (responseData.IsValid() == false) {
                throw new InvalidSessionData();
            }

            if (responseData.isActive) {
                return newSession;
            }

            throw new SessionConnectFail($"Failed to connect to the session. || {host} || {port}");
        }

        throw new NotImplementHandlerException(typeof(TcpJsonSessionConnect));
    }

    protected override async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token) {
        var bytes = await ReadBytesAsync(session, TCP_HEADER_SIZE, token);
        var header = bytes.ToStruct<TcpHeader>();
        if (header.HasValue) {
            if (header.Value.IsValid() == false) {
                throw new InvalidHeaderException(header);
            }
            
            return header.Value;
        }

        throw new InvalidHeaderException();
    }

    public override async Task ReceiveDataAsync(TcpSession session, TcpHeader header, CancellationToken token) {
        if (TcpJsonHandlerProvider.inst.TryGetHandler(header.body, out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            await handler.ReceiveAsync(session, bytes, token);
        } else {
            throw new NotImplementHandlerException(header.GetEnumBody<TCP_BODY>());
        }
    }

    protected override async Task<bool> SendDataAsync<T>(TcpSession session, T data, CancellationToken token) {
        if (TryGetHandler(data, out var handler)) {
            await handler.SendAsync(session, data.ToBytes(), token);
            return true;
        }

        throw new NotImplementHandlerException(data);
    }

    public override async Task<T> ReceiveDataAsync<T>(TcpSession session, TcpHeader header, CancellationToken token) {
        if (TcpJsonHandlerProvider.inst.TryGetReceiveHandler<T>(out var handler)) {
            var bytes = await ReadBytesAsync(session, header.length, token);
            return await handler.ReceiveBytesAsync(session, bytes, token);
        }

        throw new NotImplementHandlerException(typeof(T));
    }

    protected override ITcpHandler GetHandler(TcpJsonPacket data) => TcpJsonHandlerProvider.inst.TryGetHandler(data.GetType(), out var handler) ? handler : null;
    
    protected override byte[] GetBytes(TcpJsonPacket data) => data.ToBytes();

    protected override uint CreateSessionId() => (uint) new Random(DateTime.Now.GetHashCode()).Next();
    
    public async void StartTest(CancellationToken token) {
        if (session.Connected) {
            var data = new TcpJsonTestRequest {
                sessionId = session.ID,
                requestText = "Hello World!!",
            };

            await SendDataPublishAsync(session, data);
            Logger.TraceLog($"Send {nameof(data)} || {data.sessionId} || {data.requestText}", Color.magenta);
        }
    }
}