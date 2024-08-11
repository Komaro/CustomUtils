using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.PerformanceTesting;

[TestFixture]
public class EditModeTestRunner {
    
    // [TestCase(1)]
    // [TestCase(2)]
    // public void CaseTest(int value) {
    //     Logger.TraceLog($"{nameof(CaseTest)} param || {value}");
    // }

    #region [Enum]
    
    [Test]
    [Performance]
    public void EnumParsePerformanceTest() {
        var enumToIntGroup = new SampleGroup("EnumToInt");
        var intToEnumGroup = new SampleGroup("IntToEnum");
        var stringToEnumGroup = new SampleGroup("StringToEnum");
        
        var enumValue = TCP_BODY.TEST;
        var intValue = (int) enumValue;
        var stringValue = enumValue.ToString();

        Measure.Method(() => EnumUtil.ConvertFast(enumValue)).WarmupCount(10).MeasurementCount(10).IterationsPerMeasurement(10000).SampleGroup(enumToIntGroup).GC().Run();
        Measure.Method(() => EnumUtil.ConvertFast<TCP_BODY>(intValue)).WarmupCount(10).MeasurementCount(10).IterationsPerMeasurement(10000).SampleGroup(intToEnumGroup).GC().Run();
        Measure.Method(() => EnumUtil.Convert<TCP_BODY>(stringValue)).WarmupCount(10).MeasurementCount(10).IterationsPerMeasurement(10000).SampleGroup(stringToEnumGroup).GC().Run();
    }
    
    [Test]
    [Performance]
    public void EnumCastPerformanceTest() {
        var directGroup = new SampleGroup("Direct", SampleUnit.Microsecond);
        var castGroup = new SampleGroup("Cast", SampleUnit.Microsecond);
        
        Measure.Method(() => {
            _ = new EnumIntStruct((int) TCP_BODY.CONNECT, (int) TCP_ERROR.INVALID_SESSION_DATA);
        }).WarmupCount(15).MeasurementCount(10).IterationsPerMeasurement(10000).GC().SampleGroup(directGroup).Run();
        
        Measure.Method(() => {
            _ = new EnumIntStruct(TCP_BODY.CONNECT, TCP_ERROR.INVALID_SESSION_DATA);
        }).WarmupCount(15).MeasurementCount(10).IterationsPerMeasurement(10000).GC().SampleGroup(castGroup).Run();
    }

    private struct EnumIntStruct {

        public int body;
        public int error;

        public EnumIntStruct(int body, int error) {
            this.body = body;
            this.error = error;
        }
        
        public EnumIntStruct(Enum body, Enum error) {
            this.body = Convert.ToInt32(body);
            this.error = Convert.ToInt32(error);
        }
    }
    
    #endregion

    #region [Json]
    
    [TestCase(2500)]
    [Performance]
    public void JsonPerformanceTest(int count) {
        var serializeGroup = new SampleGroup("Serialize");
        var requestGroup = new SampleGroup("Request");
        var requestStreamGroup = new SampleGroup("RequestStream");
        var responseGroup = new SampleGroup("Response");
        var responseStreamGroup = new SampleGroup("ResponseStream");
        
        var request = new TcpJsonRequestSessionPacket(9851153);
        var requestString = JsonConvert.SerializeObject(request);
        var requestBytes = requestString.ToBytes();

        Measure.Method(() => {
            _ = JsonConvert.SerializeObject(request);
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(serializeGroup).Run();
        
        Measure.Method(() => {
            _ = requestString.ToBytes();
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(requestGroup).Run();
        
        Measure.Method(() => {
            var memoryBuffer = new byte[requestString.GetByteCount()];
            using (var memoryStream = new MemoryStream(memoryBuffer))
            using (var writer = new StreamWriter(memoryStream)) {
                writer.Write(requestString);
                writer.Flush();
            }
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(requestStreamGroup).Run();
        
        Measure.Method(() => {
            var responseString = requestBytes.GetString();
            _ = JsonConvert.DeserializeObject<TcpJsonRequestSessionPacket>(responseString);
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(responseGroup).Run();
        
        Measure.Method(() => {
            using (var memoryStream = new MemoryStream(512)) {
                memoryStream.Write(requestBytes);
                using (var reader = new StreamReader(memoryStream, Encoding.ASCII))
                using (var jsonReader = new JsonTextReader(reader)) {
                    _ = new JsonSerializer().Deserialize<TcpJsonRequestSessionPacket>(jsonReader);
                }
            }
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(responseStreamGroup).Run();
    }
    
    #endregion

    #region [Unsafe]

    [Test]
    [Performance]
    public void UnsafePerformanceTest() {
        var unsafeGroup = new SampleGroup("Unsafe");
        var covertGroup = new SampleGroup("Convert");
        var duplicateCastGroup = new SampleGroup("DuplicateCast");
        var enumToIntLambdaGroup = new SampleGroup("LambdaExpressEnumToInt");
        var intToEnumLambdaGroup = new SampleGroup("LambdaExpressIntToEnum");

        var test = LambdaExpressionProvider.GetIntToEnumFunc<TCP_BODY>().Invoke(102);
        Logger.TraceLog(test.ToString());

        Measure.Method(() => TCP_BODY.TEST.ToIntUnsafe()).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(unsafeGroup).Run();
        Measure.Method(() => _ = EnumUtil.Convert(TCP_BODY.TEST)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(covertGroup).Run();
        Measure.Method(() => _ = (int)(object)TCP_BODY.TEST).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(duplicateCastGroup).Run();
        Measure.Method(() => _ = LambdaExpressionProvider.GetEnumToIntFun<TCP_BODY>().Invoke(TCP_BODY.TEST)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(enumToIntLambdaGroup).Run();
        Measure.Method(() => _ = LambdaExpressionProvider.GetIntToEnumFunc<TCP_BODY>().Invoke(102)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(intToEnumLambdaGroup).Run();
    }
    
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

    #region [Marshal]

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
