using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
[Category(TestConstants.Category.PERFORMANCE)]
public class CollectionTestRunner {

    public class SelectManyTest {

        public int[] values;
    }

    [TestCase(50, 20)]
    [TestCase(50, 50)]
    [TestCase(50, 100)]
    [TestCase(50, 200)]
    [TestCase(50, 2000)]
    [Performance]
    public void CollectionPerformanceTest(int measurementCount, int count) {
        var whileGroup = new SampleGroup("While", SampleUnit.Microsecond);
        var forGroup = new SampleGroup("For", SampleUnit.Microsecond);

        Measure.Method(() => {
            var syncCount = 10;
            while (syncCount-- > 0) {
                _ = syncCount + 1;
            }
        }).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(whileGroup).GC().Run();

        Measure.Method(() => {
            var syncCount = 10;
            for (var i = 0; i < syncCount; i++) {
                _ = syncCount + 1;
            }
        }).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
    }

    [TestCase(100, 100)]
    [Performance]
    public void ToArrayPerformanceTest(int measurementCount, int count) {
        var selectGroup = new SampleGroup("Select");
        var forGroup = new SampleGroup("For");

        var randomList = RandomUtil.GetRandoms(20).ToList();
        var intArray = randomList.ToArray();
        Assert.IsNotEmpty(intArray);

        var longArray = intArray.Select(value => (long)value).ToArray();
        Assert.IsNotEmpty(longArray);

        longArray = intArray.ToArray(value => (long)value);
        Assert.IsNotNull(longArray);

        Measure.Method(() => _ = intArray.Select(value => _ = (long)value).ToArray()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(selectGroup).GC().Run();
        Measure.Method(() => _ = intArray.ToArray(value => (long)value)).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
    }

    [TestCase(10, 100)]
    [Performance]
    public void FindAllPerformanceTest(int measurementCount, int count) {
        var findAllGroup = new SampleGroup("FindAll");
        var whereToArrayGroup = new SampleGroup("WhereToArray");
        var whereToListGroup = new SampleGroup("WhereToList");
        
        var originList = new List<int>(RandomUtil.GetRandoms(100, 1, 10));
        Measure.Method(() => _ = originList.FindAll(x => x > 5)).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(findAllGroup).GC().Run();
        Measure.Method(() => _ = originList.Where(x => x > 5).ToArray()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(whereToArrayGroup).GC().Run();
        Measure.Method(() => _ = originList.Where(x => x > 5).ToList()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(whereToListGroup).GC().Run();
    }
    
    [Test]
    public void CollectionUtilTest() {
        var empty_01 = CollectionUtil.Empty<List<int>, int>();
        Assert.IsNotNull(empty_01);

        var empty_02 = CollectionUtil.Empty<List<int>, int>();
        Assert.IsNotNull(empty_02);

        Assert.AreEqual(empty_01, empty_02);
        Assert.AreEqual(empty_01.GetHashCode(), empty_02.GetHashCode());

        var empty_03 = CollectionUtil.List.Empty<int>();
        Assert.IsNotNull(empty_03);

        Assert.AreEqual(empty_01, empty_03);
        Assert.AreEqual(empty_01.GetHashCode(), empty_03.GetHashCode());
    }
}