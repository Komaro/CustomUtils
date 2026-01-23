using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
[Category(TestConstants.Category.PERFORMANCE)]
public class ImmutableTestRunner {
    
    [Performance]
    [TestCase(10, 1000)]
    public void ImmutableArrayPerformanceTest(int measurementCount, int count) {
        var arrayForeachGroup = new SampleGroup("ArrayForeach", SampleUnit.Microsecond);
        var arrayForGroup = new SampleGroup("ArrayFor", SampleUnit.Microsecond);
        var immutableArrayForeachGroup = new SampleGroup("ImmutableArrayForeach", SampleUnit.Microsecond);
        var immutableArrayForGroup = new SampleGroup("ImmutableArrayFor", SampleUnit.Microsecond);
        
        var array = RandomUtil.GetRandoms(100).ToArray();
        var immutableArray = array.ToImmutableArray();
        Measure.Method(() => {
            foreach (var i in array) {
                _ = i;
            }
        }).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).GC().SampleGroup(arrayForeachGroup).Run();;
        
        Measure.Method(() => {
            for (var i = 0; i < array.Length; i++) {
                _ = array[i];
            }
        }).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).GC().SampleGroup(arrayForGroup).Run();;

        Measure.Method(() => {
            foreach (var i in immutableArray) {
                _ = i;
            }
        }).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).GC().SampleGroup(immutableArrayForeachGroup).Run();;
        
        Measure.Method(() => {
            for (int i = 0; i < immutableArray.Length; i++) {
                _ = immutableArray[i];
            }
        }).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).GC().SampleGroup(immutableArrayForGroup).Run();;
    }
}
