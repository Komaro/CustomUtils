using System;
using NUnit.Framework;
using Unity.PerformanceTesting;

public class EnumTestRunner {

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
