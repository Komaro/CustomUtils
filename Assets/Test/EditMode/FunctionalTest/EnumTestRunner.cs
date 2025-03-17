using System;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
public class EnumTestRunner {

    private enum SBYTE_ENUM : sbyte {
        SBYTE_MIN = sbyte.MinValue,
        SBYTE,
        SBYTE_MAX = sbyte.MaxValue,
    }
    
    private enum BYTE_ENUM : byte {
        BYTE_MIN = byte.MinValue,
        BYTE,
        BYTE_MAX = byte.MaxValue,
    }

    private enum INT_16_ENUM : short {
        INT_16_MIN = short.MinValue,
        INT_16,
        INT_16_MAX = short.MaxValue,
    }
    
    private enum UINT_16_ENUM : ushort {
        UINT_16_MIN = ushort.MinValue,
        UINT_16,
        UINT_16_MAX = ushort.MaxValue,
    }

    private enum INT_ENUM {
        INT_MIN = int.MinValue,
        INT,
        INT_MAX = int.MaxValue
    }
    
    private enum UINT_ENUM : uint {
        UINT_MIN = uint.MinValue,
        UINT,
        UINT_MAX = uint.MaxValue
    }

    private enum INT_64_ENUM : long {
        INT_64_MIN = long.MinValue,
        INT_64,
        INT_64_MAX = long.MaxValue,
    }
    
    private enum UINT_64_ENUM : ulong {
        UINT_64_MIN = ulong.MinValue,
        UINT_64,
        UINT_64_MAX = ulong.MaxValue,
    }
    
    [Test]
    public void AllEnumGetValuesExceptionTest() {
        Assert.DoesNotThrow(() => {
            foreach (var enumType in ReflectionProvider.GetEnums()) {
                try {
                    _ = EnumUtil.GetValues(enumType);
                } catch (Exception e) {
                    Logger.TraceLog(enumType.FullName);
                    throw;
                }
            }
        });
    }
    
    private class GenericClass<T> {

        public enum GENERIC_CLASS_ENUM_TYPE {
            NONE,
            FIRST,
            SECOND,
        } 
    }

    [Test]
    public void GenericClassEnumMakeTest() {
        var enumType = typeof(GenericClass<>.GENERIC_CLASS_ENUM_TYPE);
        Assert.IsTrue(enumType.IsGenericTypeDefinition);

        var intMakeType = enumType.MakeGenericType(typeof(int));
        var longMakeType = enumType.MakeGenericType(typeof(long));
        Assert.IsTrue(intMakeType.GetGenericTypeDefinition() == longMakeType.GetGenericTypeDefinition());
        Assert.IsTrue(intMakeType != longMakeType);

        var intEnums = EnumUtil.GetValues(intMakeType).ToArray();
        var longEnums = EnumUtil.GetValues(longMakeType).ToArray();
        Assert.IsTrue(intEnums.Length == longEnums.Length);

        for (var i = 0; i < intEnums.Length; i++) {
            var intEnum = intEnums[i];
            var intEnumType = intEnum.GetType();
            Logger.TraceLog(intEnumType.UnderlyingSystemType.GetCleanFullName());
            Assert.IsTrue(intEnumType.UnderlyingSystemType.GetGenericTypeDefinition() != null);

            var longEnum = longEnums[i];
            var longEnumType = longEnum.GetType();
            Logger.TraceLog(longEnumType.UnderlyingSystemType.GetCleanFullName());
            Assert.IsTrue(longEnumType.UnderlyingSystemType.GetGenericTypeDefinition() != null);

            Assert.IsFalse(intEnumType.UnderlyingSystemType == longEnumType.UnderlyingSystemType);
            Assert.IsTrue(intEnumType.UnderlyingSystemType.GetGenericTypeDefinition() == longEnumType.UnderlyingSystemType.GetGenericTypeDefinition());

            Assert.IsTrue(intEnum.Equals(longEnum) == false);
            Assert.IsTrue(intEnumType != longEnumType);
            
            Logger.Log("");
        }
    }
    
    
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(2500)]
    [Performance]
    public void EnumBagPerformanceTest(int count) {
        var enumGroup = new SampleGroup("Enum");
        var typeGroup = new SampleGroup("Type");
        var optimizeTypeGroup = new SampleGroup("OptimizeType");
        var optimizeTypeGroupV2 = new SampleGroup("OptimizeTypeV2");
        var genericGroup = new SampleGroup("Generic");
        const int measurementCount = 200;
        
        Measure.Method(() => _ = Enum.GetValues(typeof(TCP_BODY))).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(enumGroup).Run();
        Measure.Method(() => _ = EnumUtil.GetValues(typeof(TCP_BODY))).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(typeGroup).Run();
        Measure.Method(() => _ = EnumUtil.GetValues<TCP_BODY>()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(genericGroup).Run();
    }

    [Test]
    [Performance]
    public void EnumParsePerformanceTest() {
        var enumToIntGroup = new SampleGroup("EnumToInt");
        var enumToIntFastGroup = new SampleGroup("EnumToIntFast");
        
        var intToEnumGroup = new SampleGroup("IntToEnum");
        var intToEnumFastGroup = new SampleGroup("IntToEnumFast");
        
        var stringToEnumGroup = new SampleGroup("StringToEnum");
        var stringToEnumBagGroup = new SampleGroup("StringToEnumBag");

        var getValuesGroup = new SampleGroup("GetValues");
        var getValueListGroup = new SampleGroup("GetValueList");
        var getUtilsValueListGroup = new SampleGroup("GetUtilsValueList");

        var enumValue = TCP_BODY.TEST_REQUEST;
        var intValue = (int) enumValue;
        var stringValue = enumValue.ToString();

        var count = 500;
        Measure.Method(() => _ = EnumUtil.Convert(enumValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(enumToIntGroup).GC().Run();
        Measure.Method(() => _ = EnumUtil.ConvertFast(enumValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(enumToIntFastGroup).GC().Run();
        
        Measure.Method(() => _ = EnumUtil.Convert<TCP_BODY>(intValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(intToEnumGroup).GC().Run();
        Measure.Method(() => _ = EnumUtil.ConvertFast<TCP_BODY>(intValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(intToEnumFastGroup).GC().Run();
        
        Measure.Method(() => _ = EnumUtil.Convert<TCP_BODY>(stringValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(stringToEnumGroup).GC().Run();
        Measure.Method(() => _ = EnumUtil.ConvertFast<TCP_BODY>(stringValue)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(stringToEnumBagGroup).GC().Run();

        Measure.Method(() => _ = enumValue.GetValues(true, true)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(getValuesGroup).GC().Run();
        Measure.Method(() => _ = enumValue.GetValueList(true, true)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(getValueListGroup).GC().Run();
        Measure.Method(() => _ = EnumUtil.GetValueList<TCP_BODY>(true, true)).WarmupCount(10).MeasurementCount(15).IterationsPerMeasurement(count).SampleGroup(getUtilsValueListGroup).GC().Run();
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
}
