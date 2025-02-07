using NUnit.Framework;
using Unity.PerformanceTesting;

[TestFixture]
[Category("Reflection")]
[Category("Performance")]
public class ReflectionTestRunner {
    
    /*
     * Expression 구현과 DynamicMethod 구현이 Default에 비해 수치상 5배 정도 빠름
     * Expression과 DynamicMethod의 경우 DynamicMethod의 경우가 미세하게 우위
     * 표준편차는 양쪽 다 경우에 따라 서로 높거나 낮거나 하기 때문에 평균적으로는 거의 차이가 없는 것으로 결론
     * 추가적인 테스트 결과 속도 차이는 Func을 가져오는 처리에서 오는 것으로 보임. 테스트 조건을 수정해서 다시 테스트 할 필요가 있음
     */
    [Performance]
    [TestCase(100, 5)]
    [TestCase(100, 10)]
    [TestCase(1000, 5)]
    [TestCase(1000, 10)]
    [TestCase(10000, 5)]
    [TestCase(10000, 5)]
    [TestCase(10000, 10)]
    [TestCase(10000, 100)]
    [TestCase(10000, 200)]
    [TestCase(10000, 500)]
    public void ReflectionPerformanceTest(int iterationCount, int measurementCount) {
        var warmupCount = 5;
        var defaultGroup = new SampleGroup("Default", SampleUnit.Microsecond);
        var expressionGroup = new SampleGroup("Expression", SampleUnit.Microsecond);
        var dynamicMethodGroup = new SampleGroup("DynamicMethod", SampleUnit.Microsecond);

        var testClass = new ReflectionTestClass { GetInt = RandomUtil.GetRandom(0, 9999999)};
        
        // Default
        var info = typeof(ReflectionTestClass).GetField(nameof(ReflectionTestClass.GetInt));
        Assert.AreEqual(testClass.GetInt, info.GetValue(testClass));
        Measure.Method(() => _ = info.GetValue(testClass)).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(defaultGroup).GC().Run();

        // Expression
        var expression = ExpressionProvider.GetFieldValueFunc(info, testClass);
        Assert.AreEqual(testClass.GetInt, expression.Invoke(testClass));
        Measure.Method(() => _ = expression.Invoke(testClass)).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(expressionGroup).GC().Run();

        // Dynamic Method
        var getIntDelegate = DynamicMethodProvider.GetFieldValueFunc(testClass, info);
        Assert.AreEqual(testClass.GetInt, getIntDelegate.Invoke(testClass));
        Measure.Method(() => _ = getIntDelegate.Invoke(testClass)).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(dynamicMethodGroup).GC().Run();
    }

    /*
     * 테스트 결과 GetValue 자체의 시간보다 FieldInfo를 획득하는 데 걸리는 시간의 비중이 큼
     * 추가적으로 캐싱되어 있는 Func을 가져오는데 걸리는 시간도 적지 않은 시간을 차지함
     * 이로 인해 단순히 DynamicMethod를 호출하는 것만으로 유의미한 성능 향상은 기대하기 어려움
     * 유의미한 성능 향상을 위해선 Func 자체를 최종적으로 캐싱하여야 함
     * 이 경우 캐싱을 이중으로 하는 문제가 발생하고 만약 여러개의 Func이 있다면 의미 없는 처리가 될 수 있음
     * 여기서 좀 더 유의미한 성능 향상을 위해선 FieldInfo를 획득하는 시간을 최적화 하여야 하는 데 이는 추가적인 캐싱 소요를 유발함
     */
    [Performance]
    [TestCase(10, 10)]
    [TestCase(100, 100)]
    [TestCase(100, 150)]
    [TestCase(100, 200)]
    public void ReflectionExtensionPerformanceTest(int iterationCount, int measurementCount) {
        var warmupCount = 5;
        var defaultGroup = new SampleGroup("Default", SampleUnit.Microsecond);
        var fastGroup = new SampleGroup("Fast", SampleUnit.Microsecond);
        var fastInfoCacheGroup = new SampleGroup("FastInfoCache", SampleUnit.Microsecond);
        var fastFuncCacheGroup = new SampleGroup("FastFuncCache", SampleUnit.Microsecond);

        var testClass = new ReflectionTestClass { GetInt = RandomUtil.GetRandom(1, 99999999) };
        var type = typeof(ReflectionTestClass);
        var info = testClass.GetType().GetField(nameof(ReflectionTestClass.GetInt));
        var func = DynamicMethodProvider.GetFieldValueFunc(testClass, info);
        
        Measure.Method(() => _ = type.GetFieldValue(testClass, nameof(ReflectionTestClass.GetInt))).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(defaultGroup).GC().Run();
        Measure.Method(() => _ = type.GetFieldValueFast(testClass, nameof(ReflectionTestClass.GetInt))).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(fastGroup).GC().Run();
        Measure.Method(() => _ = DynamicMethodProvider.GetFieldValueFunc(testClass, info).Invoke(testClass)).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(fastInfoCacheGroup).GC().Run();
        Measure.Method(() => _ = func.Invoke(testClass)).WarmupCount(warmupCount).MeasurementCount(measurementCount).IterationsPerMeasurement(iterationCount).SampleGroup(fastFuncCacheGroup).GC().Run();
    }
    
    private class ReflectionTestClass {

        public int GetInt;
    }
}
