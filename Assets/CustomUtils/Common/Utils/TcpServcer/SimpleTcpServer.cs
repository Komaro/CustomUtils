using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleTcpServer : IDisposable {

    private readonly TcpListener _listener;
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    
    private CancellationTokenSource _listenerCancelToken;

    private bool _isRunning;

    private const int TEMP_PORT = 8890;

    public SimpleTcpServer() {
        _listener = new TcpListener(new IPEndPoint(IPAddress.Any, TEMP_PORT));
    }

    public SimpleTcpServer(IPAddress ipAddress, int port) {
        _listener = new TcpListener(new IPEndPoint(ipAddress, port));
    }

    ~SimpleTcpServer() => Dispose();
    
    public void Dispose() {
        if (_isRunning) {
            Stop();
            _memoryPool.Dispose();
        }
    }

    public void Start() {
        if (IsRunning()) {
            Stop();
        }
        
        _listener.Start(10);
        _isRunning = true;
        try {
            _listenerCancelToken = new CancellationTokenSource();
            _ = Task.Run(() => Run(_listenerCancelToken.Token), _listenerCancelToken.Token);
            Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Start)} || {_listener.LocalEndpoint}", Color.green);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            Stop();
        }
    }

    public void Stop() {
        _listenerCancelToken.Cancel();
        _listener.Stop();
        _isRunning = false;
        Logger.TraceLog($"{nameof(SimpleTcpServer)} {nameof(Stop)}", Color.yellow);
    }
    
    private async void Run(CancellationToken token) {
        while (_isRunning) {
            var acceptTask = _listener.AcceptTcpClientAsync();
            acceptTask.Wait(token);
            if (token.IsCancellationRequested) {
                Logger.TraceLog($"Receive Cancellation Request", Color.red);
                break;
            }
            
            Logger.TraceLog("Connected", Color.green);
            var clientTask = Task.Run(() => ClientHandler(acceptTask.Result, token), token);
        }
    }

    private async void ClientHandler(TcpClient client, CancellationToken token) {
        try {
            var stream = client.GetStream();
            while (client.Connected && token.IsCancellationRequested == false) {
                var dataLength = 0;
                using (var memoryOwner = _memoryPool.Rent(4)) {
                    var buffer = memoryOwner.Memory;
                    var bytesLength = await stream.ReadAsync(buffer, token);
                    if (bytesLength == 0 || token.IsCancellationRequested) {
                        Logger.TraceLog("Disconnected", Color.yellow);
                        break;
                    }

                    dataLength = BitConverter.ToInt32(buffer.ToArray(), 0);
                }
                
                using (var memoryOwner = _memoryPool.Rent(1024)) {
                    using var memoryStream = new MemoryStream();
                    var buffer = memoryOwner.Memory;
                    var totalReadBytesLength = 0;
                    while (totalReadBytesLength < dataLength) {
                        var readBytes = await stream.ReadAsync(buffer, token);
                        if (readBytes == 0 || token.IsCancellationRequested) {
                            Logger.TraceLog("Disconnected", Color.yellow);
                            break;
                        }

                        await memoryStream.WriteAsync(buffer[..readBytes], token);
                        totalReadBytesLength += readBytes;
                    }
                    
                    var bytes = memoryStream.ToArray();
                    Logger.TraceLog(Encoding.UTF8.GetString(bytes));

                    await memoryStream.DisposeAsync();
                }
            }
        } catch (Exception) {
            throw;
        }
    }

    public bool IsRunning() => _isRunning;
}
