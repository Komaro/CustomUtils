
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    [Performance]
    public void CollectionPerformanceTest(int measurementCount, int count) {
        var castGroup = new SampleGroup("CastToList");
        var forGroup = new SampleGroup("ForToList");
        var linqGroup = new SampleGroup("LinqToList");

        IList list = RandomUtil.GetRandoms(50).ToList();

        Measure.Method(() => _ = list.ToList<int>()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(castGroup).GC().Run();
        Measure.Method(() => _ = list.ToList(obj => (int) obj)).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
    }
    
    [TestCase(100, 100)]
    [Performance]
    public void ToArrayPerformanceTest(int measurementCount, int count) {
        var selectGroup = new SampleGroup("Select");
        var forGroup = new SampleGroup("For");

        var randomList = RandomUtil.GetRandoms(20).ToList();
        var intArray = randomList.ToArray();
        Assert.IsNotEmpty(intArray);
        
        var longArray = intArray.Select(value => (long) value).ToArray();
        Assert.IsNotEmpty(longArray);

        longArray = intArray.ToArray(value => (long) value);
        Assert.IsNotNull(longArray);
        
        Measure.Method(() => _ = intArray.Select(value => _ = (long) value).ToArray()).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(selectGroup).GC().Run();
        Measure.Method(() => _ = intArray.ToArray(value => (long) value)).WarmupCount(5).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
    }
}
