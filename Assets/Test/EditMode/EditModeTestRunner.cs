using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.Graphs;
using UnityEngine;

public class EditModeTestRunner {
    
    // [TestCase(1)]
    // [TestCase(2)]
    // public void CaseTest(int value) {
    //     Logger.TraceLog($"{nameof(CaseTest)} param || {value}");
    // }

    private static SimpleTcpServer _server;
    
    [Test]
    public async Task SimpleTcpServerTest() {
        _server ??= new SimpleTcpServer();
        _server.ChangeServeModule(new TcpSimpleServeModule(_server));
        _server.Start();

        var source = new CancellationTokenSource();
        var client = new TcpClient();
        await client.ConnectAsync("localhost", 8890);
        if (client.Connected) {
            var session = new TcpSession(client, 9999u);
            var stream = session.Stream;
            var connect = new TcpRequestConnect(session);
            await stream.WriteAsync(connect.ToBytes(), source.Token);

            using (var owner = MemoryPool<byte>.Shared.Rent(Marshal.SizeOf<TcpHeader>())) {
                var buffer = owner.Memory;
                var bytesLength = await stream.ReadAsync(buffer, source.Token);
                if (bytesLength <= 0) {
                    throw new DisconnectSessionException(session);
                }
                
                var header = buffer.ToStruct<TcpHeader>();
                if (header is { error: TCP_ERROR.NONE }) {
                    _ = Task.Run(() => ReceiveAsync(session, source.Token), source.Token);
                    if (TcpHandlerProvider.TryGetSendHandler<TcpRequestTest>(out var handler)) {
                        await handler.SendAsync(session, new TcpRequestTest(15), source.Token);
                    }
                }
            }
        }

        await Task.Delay(5000, source.Token);

        source.Cancel();
        client.Dispose();
        _server.Dispose();
    }

    private async Task ReceiveAsync(TcpSession session, CancellationToken token) {
        var stream = session.Stream;
        using (var owner = MemoryPool<byte>.Shared.Rent(1024)) {
            var buffer = owner.Memory;
            while (session.Connected) {
                var header = await ReceiveHeaderAsync(session);
                if (header.sessionId != session.ID) {
                    throw new InvalidDataException();
                }

                if (TcpHandlerProvider.TryGetReceiveHandler(header.bodyType, out var handler)) {
                    var totalReadLength = 0;
                    using (var memoryStream = new MemoryStream()) {
                        while (totalReadLength < header.byteLength) {
                            var readLength = await stream.ReadAsync(buffer, token);
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
}
