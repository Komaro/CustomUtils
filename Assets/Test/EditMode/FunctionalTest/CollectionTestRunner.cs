
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
[Category(TestConstants.Category.PERFORMANCE)]
public class CollectionTestRunner {


    [TestCase(100, 100)]
    [Performance]
    public void ToArrayPerformanceTest(int measurementCount, int count) {
        var converterGroup = new SampleGroup("Converter");
        var selectGroup = new SampleGroup("Select");
        var forGroup = new SampleGroup("For");

        var randomList = RandomUtil.GetRandoms(20).ToList();
        var intArray = randomList.ToArray();
        Assert.IsNotEmpty(intArray);
        
        var longArray = intArray.ConvertTo(value => (long) value).ToArray();
        Assert.IsNotEmpty(longArray);

        longArray = intArray.Select(value => (long) value).ToArray();
        Assert.IsNotEmpty(longArray);

        longArray = new long[intArray.Length];
        for (var i = 0; i < intArray.Length; i++) {
            longArray[i] = intArray[i];
        }
        Assert.IsNotNull(longArray);
        
        Measure.Method(() => _ = intArray.ConvertTo(value => (long)value).ToArray()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(converterGroup).GC().Run();
        Measure.Method(() => _ = intArray.Select(value => (long) value).ToArray()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(selectGroup).GC().Run();
        Measure.Method(() => {
            longArray = new long[intArray.Length];
            for (var i = 0; i < intArray.Length; i++) {
                longArray[i] = intArray[i];
            }
        }).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
    }
}
