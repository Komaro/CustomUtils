using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.PERFORMANCE)]
public class PerformanceTestRunner {

    [Performance]
    [TestCase(5000, 30, 10, Description = "어지간히 큰 값이 아닌 경우 순차처리가 유리")]
    public void ParallelPerformanceTest(int randomLength, int measurementCount, int count) {
        var forGroup = new SampleGroup("For");
        var parallelGroup = new SampleGroup("ParallelFor");

        var dictionary = new Dictionary<int, Queue<int>>();
        for (var key = 0; key < 10; key++) {
            var queue = dictionary.AutoAdd(key);
            foreach (var random in RandomUtil.GetRandoms(randomLength)) {
                queue.Enqueue(random);
            }
        }

        Measure.Method(() => {
           foreach (var (_, queue) in dictionary) {
                var loopCount = queue.Count;
                while (loopCount-- > 0) {
                    if (queue.TryDequeue(out var value)) {
                        queue.Enqueue(value);
                    }
                }
           } 
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(forGroup).GC().Run();
        
        Measure.Method(() => {
            dictionary.Select(pair => pair.Value).AsParallel().ForAll(queue => {
                var loopCount = queue.Count;
                while (loopCount-- > 0) {
                    if (queue.TryDequeue(out var value)) {
                        queue.Enqueue(value);
                    }
                }
            });
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(parallelGroup).GC().Run();
    }

    [Performance]
    [TestCase(20, 10000)]
    [TestCase(20, 20000)]
    [TestCase(20, 50000)]
    [TestCase(20, 100000)]
    [TestCase(20, 500000)]
    [TestCase(20, 1000000)]
    public void IfAndModuloPerformanceTest(int measurementCount, int count) {
        var ifGroup = new SampleGroup("If");
        var moduloGroup = new SampleGroup("Modulo");
        var randomInt = RandomUtil.GetRandom(1, 100);

        var ifInt = 0;
        Measure.Method(() => {
            ifInt++;
            if (ifInt == randomInt) {
                ifInt = 0;
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(ifGroup).Run();

        var moduloInt = 0;
        Measure.Method(() => {
            moduloInt++;
            moduloInt = moduloInt % randomInt;
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(moduloGroup).Run();
    }

    [Performance]
    [TestCase(20, 10000)]
    [TestCase(20, 20000)]
    [TestCase(20, 50000)]
    [TestCase(20, 100000)]
    [TestCase(20, 500000)]
    [TestCase(20, 1000000)]
    public void InterlockedPerformanceTest(int measurementCount, int count) {
        var interLockedGroup = new SampleGroup("InterLocked", SampleUnit.Microsecond);
        var increaseGroup = new SampleGroup("Increase", SampleUnit.Microsecond);
        var addGroup = new SampleGroup("Add", SampleUnit.Microsecond);
        
        var interLockedValue = 0;
        Measure.Method(() => {
            Interlocked.Increment(ref interLockedValue);
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(interLockedGroup).CleanUp(() => interLockedValue = 0).Run();

        var increaseValue = 0;
        Measure.Method(() => {
            increaseValue++;
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(increaseGroup).CleanUp(() => increaseValue = 0).Run();

        var addValue = 0;
        Measure.Method(() => {
            addValue += 1;
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(addGroup).CleanUp(() => addValue = 0).Run();
    }
}
