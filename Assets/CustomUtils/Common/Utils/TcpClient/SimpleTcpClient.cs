
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public abstract class SimpleTcpClient<THeader, TData> : ITcpClient where THeader : ITcpPacket where TData : ITcpPacket {

        protected TcpSession session;
        protected string host;
        protected int port;

        protected readonly Channel<TData> sendChannel = Channel.CreateBounded<TData>(new BoundedChannelOptions(5));
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
            if (session?.IsValid() ?? false) {
                session.Close();
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
            while (session.Connected && token.IsCancellationRequested == false) {
                token.ThrowIfCancellationRequested();
                var header = await ReceiveHeaderAsync(session, token);
                
                token.ThrowIfCancellationRequested();
                await ReceiveDataAsync(session, header, token);
            }
        }

        protected virtual async Task ReadSendChannelAsync(CancellationToken token) {
            while (session.Connected) {
                token.ThrowIfCancellationRequested();
                var data = await sendChannel.Reader.ReadAsync(token);
                
                token.ThrowIfCancellationRequested();
                await SendAsync(session, data, token);
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
        
        public abstract Task<THeader> ReceiveHeaderAsync(TcpSession session, CancellationToken token);
        public abstract Task ReceiveDataAsync(TcpSession session, THeader header, CancellationToken token);
        public abstract Task<T> ReceiveDataAsync<T>(TcpSession session, THeader header, CancellationToken token) where T : TData;
        
        public abstract Task<bool> SendAsync<T>(TcpSession session, T data, CancellationToken token) where T : TData;
        
        public bool Send<T>(TcpSession session, T data) where T : TData => Send(session.ID, data);
        public virtual bool Send<T>(uint sessionId, T data) where T : TData => (session?.VerifySession(sessionId) ?? false) && sendChannel.Writer.TryWrite(data);

        public async Task<bool> SendAsync<T>(TcpSession session, T data) where T : TData => await SendAsync(session.ID, data);
        
        public virtual async Task<bool> SendAsync<T>(uint sessionId, T data) where T : TData {
            if (session?.VerifySession(sessionId) ?? false) {
                var task = sendChannel.Writer.WriteAsync(data);
                await task;
                return task.IsCompletedSuccessfully;
            }
            
            return false;
        }
        
        protected abstract uint CreateSessionId();
    }