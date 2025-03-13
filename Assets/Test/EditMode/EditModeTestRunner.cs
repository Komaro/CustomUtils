using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.EditorCoroutines.Editor;
using Unity.PerformanceTesting;
using UnityEditor.Build.Profile;
using UnityEngine;

[TestFixture]
public class EditModeTestRunner {

    [Test]
    public void TempTest_01() {
        Logger.TraceLog($"IsMainThread? || {Service.GetService<UnityMainThreadDispatcherService>().IsMainThread()}");
        var progress = new Progress<float>(progress => Logger.TraceLog($"{Service.GetService<UnityMainThreadDispatcherService>().IsMainThread()} || {(progress / 2).ToPercent()}"));
        EditorCoroutineUtility.StartCoroutine(StartCoroutine(Constants.Resource.RESOURCE_LIST, progress), this);
        Logger.TraceLog("Success");
    }

    private IEnumerator StartCoroutine(string target, IProgress<float> progress) {
        var asyncOperation = LoadAsync(target);
        while (asyncOperation.isDone == false) {
            progress.Report(asyncOperation.progress);
            yield return null;
        }
        
        progress.Report(1f);

        if (asyncOperation.asset is TextAsset textAsset) {
            var jObject = JObject.Parse(textAsset.text);
            var totalValues = jObject.Count;
            var currentProgress = 0f;
            foreach (var pair in jObject) {
                currentProgress++;
                progress.Report(1f + currentProgress / totalValues);
                yield return null;
            }
        }
        progress.Report(2f);
    }
    
    private ResourceRequest LoadAsync(string target) => Resources.LoadAsync<TextAsset>(target);

    [Test]
    public void TempTest_02() {
        var server = new SimpleHttpServer(); // Default local host
        server.AddServeModule<AssetBundleDistributionServeModule>();
        server.Start();
        // Start Any http request
    }

    [Test]
    public void TempTest_03() {
        foreach (var profile in AssetDatabaseUtil.FindAssets<BuildProfile>(FilterUtil.CreateFilter(TypeFilter.ScriptableObject))) {
            Logger.TraceLog(profile.name);
        }
    }

    [Test]
    [Performance]
    public void TempPerformanceTest_01() {
        var group_01 = new SampleGroup("Group_01");
        var group_02 = new SampleGroup("Group_02");
        var group_03 = new SampleGroup("Group_03");
        
        var sample = "SomeData.dll";
        Measure.Method(() => {
            if (Path.HasExtension(sample)) {
                var test = Path.GetFileNameWithoutExtension(sample);
            }
        }).WarmupCount(5).MeasurementCount(10).IterationsPerMeasurement(1000).GC().SampleGroup(group_01).Run();
        
        Measure.Method(() => {
            var test = sample.GetFileNameFast();
        }).WarmupCount(5).MeasurementCount(10).IterationsPerMeasurement(1000).GC().SampleGroup(group_02).Run();

        Measure.Method(() => {
            if (Path.HasExtension(sample)) {
                var test = sample.GetFileNameFast();
            }
        }).WarmupCount(5).MeasurementCount(10).IterationsPerMeasurement(1000).GC().SampleGroup(group_03).Run();
    }
    
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(10000)]
    [TestCase(100000)]
    [TestCase(1000000)]
    [Performance]
    public void TempPerformanceTest_02(int randomCount) {
        var group_Func = new SampleGroup("Group_Func");
        var group_Predicate = new SampleGroup("Group_Predicate");

        var list = new List<int>();
        for (var i = 0; i < randomCount; i++) {
            list.Add(RandomUtil.GetRandom(int.MinValue, int.MaxValue));
        }
        
        Measure.Method(() => {
            if (list.TryFirst(out var matchValue, value => value == randomCount)) {
                Logger.TraceLog(matchValue.ToString());
            }
        }).WarmupCount(1).MeasurementCount(10).IterationsPerMeasurement(2).SampleGroup(group_Predicate).Run();
    }

    #region [Json]

    [TestCase(2500)]
    [Performance]
    public void JsonPerformanceTest(int count) {
        var serializeGroup = new SampleGroup("Serialize");
        var requestGroup = new SampleGroup("Request");
        var requestStreamGroup = new SampleGroup("RequestStream");
        var responseGroup = new SampleGroup("Response");
        var responseStreamGroup = new SampleGroup("ResponseStream");
        
        var request = new TcpJsonSessionConnect(9851153);
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
            _ = JsonConvert.DeserializeObject<TcpJsonSessionConnect>(responseString);
        }).WarmupCount(20).MeasurementCount(20).IterationsPerMeasurement(count).SampleGroup(responseGroup).Run();
        
        Measure.Method(() => {
            using (var memoryStream = new MemoryStream(512)) {
                memoryStream.Write(requestBytes);
                using (var reader = new StreamReader(memoryStream, Encoding.ASCII))
                using (var jsonReader = new JsonTextReader(reader)) {
                    _ = new JsonSerializer().Deserialize<TcpJsonSessionConnect>(jsonReader);
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

        var test = ExpressionProvider.GetIntToEnumFunc<TCP_BODY>().Invoke(102);
        Logger.TraceLog(test.ToString());

        Measure.Method(() => TCP_BODY.TEST_REQUEST.ToIntUnsafe()).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(unsafeGroup).Run();
        Measure.Method(() => _ = EnumUtil.Convert(TCP_BODY.TEST_REQUEST)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(covertGroup).Run();
        Measure.Method(() => _ = (int)(object)TCP_BODY.TEST_REQUEST).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(duplicateCastGroup).Run();
        Measure.Method(() => _ = ExpressionProvider.GetEnumToIntFun<TCP_BODY>().Invoke(TCP_BODY.TEST_REQUEST)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(enumToIntLambdaGroup).Run();
        Measure.Method(() => _ = ExpressionProvider.GetIntToEnumFunc<TCP_BODY>().Invoke(102)).MeasurementCount(20).IterationsPerMeasurement(5000).SampleGroup(intToEnumLambdaGroup).Run();
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
        
        public bool Equals(TestStruct other) => intValue == other.intValue && byteValue == other.byteValue && innerStruct.Equals(other.innerStruct);
        public override bool Equals(object obj) => obj is TestStruct other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(intValue, byteValue, innerStruct);

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
            
            public bool Equals(TestInnerStruct other) => intValue == other.intValue && byteValue == other.byteValue && doubleValue.Equals(other.doubleValue) && doubleValue2.Equals(other.doubleValue2);
            public override bool Equals(object obj) => obj is TestInnerStruct other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(intValue, byteValue, doubleValue, doubleValue2);
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
        
        public bool Equals(TestSequentialStruct other) => intValue == other.intValue && floatValue.Equals(other.floatValue) && innerStruct.Equals(other.innerStruct);
        public override bool Equals(object obj) => obj is TestSequentialStruct other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(intValue, floatValue, innerStruct);

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
            
            public bool Equals(TestInnerStruct other) => intValue == other.intValue && byteValue == other.byteValue && doubleValue.Equals(other.doubleValue);
            public override bool Equals(object obj) => obj is TestInnerStruct other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(intValue, byteValue, doubleValue);
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
