using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class EditModeTestRunner {
    
    // [TestCase(1)]
    // [TestCase(2)]
    // public void CaseTest(int value) {
    //     Logger.TraceLog($"{nameof(CaseTest)} param || {value}");
    // }

    private static SimpleTcpServer _server;
    private static CancellationTokenSource _source;
    
    [SetUp]
    public void SetUpTcpServerTest() {
        _source?.Cancel();
        _source = new CancellationTokenSource();
        
        _server?.Dispose();
        _server = new SimpleTcpServer(IPAddress.Any, 8890);
    }

    [TearDown]
    public void TearDownTcpServerTest() {
        _source?.Cancel();
        _server?.Stop();
    }

    [Test]
    public async Task SimpleTcpServerTest() {
        _server.ChangeServeModule(new TcpPingServeModule());
        _server.Start();
        
        var client = new TcpClient();
        try {
            await client.ConnectAsync("localhost", 8890);
            
            // _ = Task.Run(() => TcpStructServeModuleTest(client, _source.Token));
            _ = Task.Run(() => TcpPingServeModuleTest(client, _source.Token));
            
            await Task.Delay(5000, _source.Token);
        } catch (SocketException ex) {
            Logger.TraceError($"{ex.SocketErrorCode.ToString()} || {ex.Message}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            client?.Close();
            await Task.CompletedTask;
        }
    }
    
    private async Task TcpPingServeModuleTest(TcpClient client, CancellationToken token) {
        using (var owner = MemoryPool<byte>.Shared.Rent(32)) {
            while (token.IsCancellationRequested == false) {
                var buffer = owner.Memory;
                var length = await client.GetStream().ReadAsync(buffer, token);
                if (length == 0) {
                    throw new DisconnectSessionException(client);
                }

                var textBytes = buffer[..length].ToArray();
                Logger.TraceLog(textBytes.GetString());

                var bytes = new byte[sizeof(bool)];
                var value = true;
                MemoryMarshal.Write(bytes, ref value);
                await client.GetStream().WriteAsync(bytes, token);
            }
        }
    }

    private async Task TcpStructServeModuleTest(TcpClient client, CancellationToken token) {
        if (client.Connected) {
            var session = new TcpSession(client, 9999u);
            var connect = new TcpRequestConnect(session);
            await session.Stream.WriteAsync(connect.ToBytes(), token);
            using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                var buffer = owner.Memory;
                var bytesLength = await session.Stream.ReadAsync(buffer, token);
                if (bytesLength <= 0) {
                    throw new DisconnectSessionException(session);
                }

                var header = buffer.ToStruct<TcpHeader>();
                if (header is { error: TCP_ERROR.NONE }) {
                    _ = Task.Run(() => ReceiveAsync(session, token), token);
                    if (TcpStructHandlerProvider.TryGetSendHandler<TcpRequestTest>(out var handler)) {
                        await handler.SendAsync(session, new TcpRequestTest(15), token);
                    }
                }
            }
        }
    }

    private async Task ReceiveAsync(TcpSession session, CancellationToken token) {
        using (var owner = MemoryPool<byte>.Shared.Rent(1024)) {
            var buffer = owner.Memory;
            while (session.Connected) {
                 var header = await ReceiveHeaderAsync(session);
                if (header.sessionId != session.ID) {
                    throw new InvalidDataException();
                }

                if (TcpStructHandlerProvider.TryGetReceiveHandler(header.bodyType, out var handler)) {
                    var totalReadLength = 0;
                    using (var memoryStream = new MemoryStream()) {
                        while (totalReadLength < header.byteLength) {
                            var readLength = await session.Stream.ReadAsync(buffer, token);
                            if (readLength <= 0) {
                                throw new DisconnectSessionException(session);
                            }

                            await memoryStream.WriteAsync(buffer[..readLength], token);
                            totalReadLength += readLength;
                        }

                        await handler.ReceiveAsync(session, memoryStream.ToArray(), token);
                    }
                }
            }
        }
    }

    private async Task<TcpHeader> ReceiveHeaderAsync(TcpSession session) {
        using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
            var buffer = owner.Memory;
            var bytesLength = await session.Stream.ReadAsync(buffer);
            if (bytesLength <= 0) {
                throw new DisconnectSessionException(session);
            }

            var header = buffer.ToStruct<TcpHeader>();
            if (header.HasValue) {
                return header.Value;
            }

            throw new InvalidCastException();
        }
    }

    #region [Unsafe Test]
    
    [Test]
    public void UnsafeTest() {
        int intValue = 594387451;
        var bytes = intValue.ToBytesUnsafe();

        int outIntValue = 0;
        outIntValue = bytes.ToUnmanagedType<int>();
        Logger.TraceLog($"{intValue} || {outIntValue}");
        Assert.IsTrue(outIntValue == intValue);
        //

        double doubleValue = 163498.64665d;
        bytes = doubleValue.ToBytesUnsafe();

        double outDoubleValue = 0;
        outDoubleValue = bytes.ToUnmanagedType<double>();
        Assert.IsTrue(doubleValue.Equals(outDoubleValue));
        Logger.TraceLog($"{doubleValue} || {outDoubleValue}\n");
        //

        TestStruct testStruct = new() {
            // intValue = 336611,
            byteValue = 12,
            innerStruct = new() {
                // intValue = 99999,
                doubleValue = 89999.55569d
            }
        };
        bytes = testStruct.ToBytesUnsafe();

        var outTestStruct = bytes.ToUnmanagedType<TestStruct>();
        Logger.TraceLog($"size || {Marshal.SizeOf<TestStruct>()} || " +
                        $"{Marshal.SizeOf(testStruct)} - {Marshal.SizeOf(testStruct.innerStruct)} || " +
                        $"{Marshal.SizeOf(outTestStruct)} - {Marshal.SizeOf(outTestStruct.innerStruct)}");
        Logger.TraceLog(testStruct.ToStringAllFields());
        Logger.TraceLog(outTestStruct.ToStringAllFields());
        Assert.IsTrue(testStruct == outTestStruct);

        if (bytes.TryUnmanagedType<TestStruct>(out var testValue)) {
            Assert.IsTrue(testStruct == testValue);
        } else {
            Assert.Fail();
        }
        //

        TestSequentialStruct testSequentialStruct = new() {
            intValue = 8888,
            floatValue = 9956.1145f,
            innerStruct = new() {
                intValue = 1111111,
                doubleValue = 5563.1145d
            }
        };
        bytes = testSequentialStruct.ToBytes();
        
        var outTestSequentialStruct = bytes.ToUnmanagedType<TestSequentialStruct>();
        Logger.TraceLog($"size || {Marshal.SizeOf<TestSequentialStruct>()} || " +
                        $"{Marshal.SizeOf(testSequentialStruct)} - {Marshal.SizeOf(testSequentialStruct.innerStruct)} || " +
                        $"{Marshal.SizeOf(outTestSequentialStruct)} - {Marshal.SizeOf(outTestSequentialStruct.innerStruct)}");
        Logger.TraceLog(testSequentialStruct.ToStringAllFields());
        Logger.TraceLog(outTestSequentialStruct.ToStringAllFields());
        Assert.IsTrue(testSequentialStruct == outTestSequentialStruct);

        if (bytes.TryUnmanagedType<TestSequentialStruct>(out var testSequentialValue)) {
            Assert.IsTrue(testSequentialStruct == testSequentialValue);
        } else {
            Assert.Fail();
        }
    }

    private struct TestStruct {
    
        public int intValue;
        public byte byteValue;
        public TestInnerStruct innerStruct;

        public static bool operator ==(TestStruct a, TestStruct b) {
            if (a.intValue != b.intValue) {
                return false;
            }

            if (a.byteValue.Equals(b.byteValue) == false) {
                return false;
            }

            if (a.innerStruct != b.innerStruct) {
                return false;
            }
            
            return true;
        }

        public static bool operator !=(TestStruct a, TestStruct b) => (a == b) == false;

        internal struct TestInnerStruct {
            
            public int intValue;
            public byte byteValue;
            public double doubleValue;
            public double doubleValue2;
            
            public static bool operator ==(TestInnerStruct a, TestInnerStruct b) {
                if (a.intValue != b.intValue) {
                    return false;
                }
                
                if (a.byteValue != b.byteValue) {
                    return false;
                }

                if (a.doubleValue.Equals(b.doubleValue) == false) {
                    return false;
                }
                
                if (a.doubleValue2.Equals(b.doubleValue2) == false) {
                    return false;
                }

                return true;
            }

            public static bool operator !=(TestInnerStruct a, TestInnerStruct b) => (a == b) == false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestSequentialStruct {
        
        public int intValue;
        public float floatValue;
        public TestInnerStruct innerStruct;
        
        public static bool operator ==(TestSequentialStruct a, TestSequentialStruct b) {
            if (a.intValue != b.intValue) {
                return false;
            }

            if (a.floatValue.Equals(b.floatValue) == false) {
                return false;
            }

            if (a.innerStruct != b.innerStruct) {
                return false;
            }
            
            return true;
        }

        public static bool operator !=(TestSequentialStruct a, TestSequentialStruct b) => (a == b) == false;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct TestInnerStruct {
            
            public int intValue;
            public byte byteValue;
            public double doubleValue;
            
            public static bool operator ==(TestInnerStruct a, TestInnerStruct b) {
                if (a.intValue != b.intValue) {
                    return false;
                }

                if (a.byteValue != b.byteValue) {
                    return false;
                }

                if (a.doubleValue.Equals(b.doubleValue) == false) {
                    return false;
                }

                return true;
            }

            public static bool operator !=(TestInnerStruct a, TestInnerStruct b) => (a == b) == false;
        }
    }
    
    #endregion

    #region [Marshal Test]

    [Test]
    public void IntMarshalTest() {
        var service = Service.GetService<StopWatchService>();
        var intValue = 453323453;
        
        service.Start();
        intValue.ToBytes();
        service.Stop();
    }

    [Test]
    public void StringMarshalTest() {
        var text = "Hello World!!";
        Span<byte> textBytes = text.ToBytes();
        Span<byte> dataBytes = new byte[4 + textBytes.Length];
        textBytes.Length.ToBytes().CopyTo(dataBytes[..4]);
        textBytes.CopyTo(dataBytes.Slice(4, textBytes.Length));

        var handle = GCHandle.Alloc(dataBytes.ToArray(), GCHandleType.Pinned);
        try {
            var ptr = handle.AddrOfPinnedObject();
            var readLength = Marshal.ReadInt32(ptr);
            Logger.TraceLog($"Read Length || {readLength}");
            Assert.IsTrue(text.Length == readLength);

            var bytes = new byte[readLength];
            Marshal.Copy(IntPtr.Add(ptr, 4), bytes, 0, readLength);

            var readText = bytes.GetString();
            Logger.TraceLog($"Read text || {bytes.GetString()}");
            Assert.IsTrue(text == readText);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            handle.Free();
        }
    }

    public enum TestEnumType {
        NONE,
        FIRST,
        SECOND,
    }
    
    #endregion
}
