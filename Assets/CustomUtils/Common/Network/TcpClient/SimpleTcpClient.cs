using System;
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public abstract class SimpleTcpClient<THeader, TData> : ITcpClient where THeader : ITcpPacket {

        protected TcpSession session;
        protected string host;
        protected int port;

        protected readonly Channel<(ITcpHandler handler, byte[] bytes)> sendChannel = Channel.CreateBounded<(ITcpHandler, byte[])>(new BoundedChannelOptions(5));
        protected readonly MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

        protected readonly CancellationTokenSource cancelToken = new();

        protected abstract int HEADER_SIZE { get; }
        
        public SimpleTcpClient(string host, int port) {
            this.host = host;
            this.port = port;
        }
        
        ~SimpleTcpClient() {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose() => Close();
        
        public virtual void Close() {
            if ((session?.IsValid() ?? false) && session.Connected) {
                session.Close();
                Logger.TraceLog($"{nameof(Close)} {nameof(TcpSession)}", Color.Red);
            } else {
                Logger.TraceLog($"{nameof(TcpSession)} is no longer valid", Color.Yellow);
            }
        }

        public async Task Start() => await Start(cancelToken.Token);

        public virtual async Task Start(CancellationToken token) {
            if (session != null && session.IsValid() && session.Connected) {
                session.Close();
            }

            var client = new TcpClient();
            if (await ConnectAsync(client)) {
                session = await ConnectSessionAsync(client, token);
                if (session.IsValid()) {
                    Logger.TraceLog($"Connected Session || {session.ID} || {session.Client.GetIpAddress()}", Color.Green);
                    _ = Task.Run(() => ReceiveDataAsync(session, token), token);
                    _ = Task.Run(() => ReadSendChannelAsync(token), token);
                }
            } else {
                throw new DisconnectSessionException(session);
            }
        }

        public async Task<bool> ConnectAsync(TcpClient client) {
            var connectTask = client.ConnectAsync(host, port);
            await connectTask;
            return connectTask.IsCompletedSuccessfully;
        }
        
        public abstract Task<TcpSession> ConnectSessionAsync(TcpClient client, CancellationToken token);
        
        protected async Task ReceiveDataAsync(TcpSession session, CancellationToken token) {
            try {
                while (session.Connected && token.IsCancellationRequested == false) {
                    token.ThrowIfCancellationRequested();
                    var header = await ReceiveHeaderAsync(session, token);

                    token.ThrowIfCancellationRequested();
                    await ReceiveDataAsync(session, header, token);
                }
            } catch (DisconnectSessionException) {
                Close();
            } catch (Exception ex) {
                Logger.TraceLog(ex.Message);
            }
        }

        protected virtual async Task ReadSendChannelAsync(CancellationToken token) {
            while (session.Connected) {
                token.ThrowIfCancellationRequested();
                var (handler, bytes) = await sendChannel.Reader.ReadAsync(token);
                
                token.ThrowIfCancellationRequested();
                await SendBytesAsync(handler, session, bytes, token);
            }
        }

        public virtual async Task<byte[]> ReadBytesAsync(TcpSession session, int length, CancellationToken token) {
            using (var owner = memoryPool.Rent(length))
            using (var stream = new MemoryStream(length)) {
                var buffer = owner.Memory;
                var totalBytesLength = 0;
                while (totalBytesLength < length) {
                    var bytesLength = await session.Stream.ReadAsync(buffer, token);
                    if (bytesLength == 0) {
                        throw new DisconnectSessionException(session);
                    }

                    token.ThrowIfCancellationRequested();

                    await stream.WriteAsync(buffer[..bytesLength], token);
                    totalBytesLength += bytesLength;
                }

                return stream.GetBuffer();
            }
        }

        protected abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
        
        public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
        public abstract Task<T> ReceiveDataAsync<T>(TcpSession session, THeader header, CancellationToken token);

        protected virtual async Task SendBytesAsync(ITcpHandler handler, TcpSession session, byte[] bytes, CancellationToken token) => await handler.SendAsync(session, bytes, token);
        protected abstract Task<bool> SendDataAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;
        
        public bool SendDataPublish<T>(TcpSession session, T data) where T : TData => SendDataPublish(session.ID, data);
        public bool SendDataPublish<T>(uint sessionId, T data) where T : TData => (session?.VerifySession(sessionId) ?? false) && sendChannel.Writer.TryWrite(CreateSendInfo(data));

        public async Task<bool> SendDataPublishAsync<T>(TcpSession session, T data) where T : TData => await SendDataPublishAsync(session.ID, data);
        
        public async Task<bool> SendDataPublishAsync<T>(uint sessionId, T data) where T : TData {
            if (session?.VerifySession(sessionId) ?? false) {
                var task = sendChannel.Writer.WriteAsync(CreateSendInfo(data));
                await task;
                return task.IsCompletedSuccessfully;
            }
            
            return false;
        }

        
        protected virtual (ITcpHandler, byte[]) CreateSendInfo(TData data) => (GetHandler(data), GetBytes(data));

        protected bool TryGetHandler(TData data, out ITcpHandler handler) => (handler = GetHandler(data)) != null;
        protected abstract ITcpHandler GetHandler(TData data);

        protected bool TryGetBytes(TData data, out byte[] bytes) => (bytes = GetBytes(data)) != Array.Empty<byte>();
        protected abstract byte[] GetBytes(TData data);
        
        protected abstract uint CreateSessionId();
    }