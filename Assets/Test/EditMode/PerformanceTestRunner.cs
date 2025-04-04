using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.PERFORMANCE)]
public class PerformanceTestRunner {

    [Performance]
    [TestCase(5000, 30, 10, Description = "어지간히 큰 값이 아닌 경우 순차처리가 유리")]
    public void QueuePerformanceTest(int randomLength, int measurementCount, int count) {
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
            dictionary.AsParallel().ForAll(pair => {
                var loopCount = pair.Value.Count;
                for (var i = 0; i < loopCount; i++) {
                    if (pair.Value.TryDequeue(out var value)) {
                        pair.Value.Enqueue(value);
                    }
                }
            });
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(parallelGroup).GC().Run();
    }
}
