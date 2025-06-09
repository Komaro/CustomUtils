using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.PerformanceTesting;

internal enum TEST_ENUM {
    FIRST = 1,
    SECOND = 2,
    THIRD = 3,
}

internal class ExpressionTestClass {
        
    public ExpressionTestClass() {
            
    }
}

[Category(TestConstants.Category.FUNCTIONAL)]
[Category(TestConstants.Category.PERFORMANCE)]
public class ExpressionTestRunner {
    
    [Performance]
    [TestCase(50, 500)]
    public void CreateConstructorPerformanceTest(int measurementCount, int count) {
        var expressionGroup = new SampleGroup("Expression");
        var createInstanceGroup = new SampleGroup("CreateInstance");
        var constructorFunc = ExpressionProvider.CreateConstructorFunc<ExpressionTestClass>();
        
        Assert.IsNotNull(constructorFunc.Invoke());
        Assert.IsNotNull(Activator.CreateInstance(typeof(ExpressionTestClass)));
        
        Measure.Method(() => constructorFunc.Invoke()).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(expressionGroup).Run();
        Measure.Method(() => Activator.CreateInstance(typeof(ExpressionTestClass))).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(createInstanceGroup).Run();
    }

    [Performance]
    [TestCase(50, 100)]
    [TestCase(50, 200)]
    [TestCase(50, 300)]
    [TestCase(50, 400)]
    [TestCase(50, 500)]
    [TestCase(50, 600)]
    [TestCase(50, 700)]
    [TestCase(50, 800)]
    [TestCase(50, 900)]
    [TestCase(50, 1000)]
    public void DelegateCastingPerformanceTest(int measurementCount, int count) {
        var directGroup = new SampleGroup("Direct", SampleUnit.Microsecond);
        var castingGroup = new SampleGroup("Casting", SampleUnit.Microsecond);
        var constructorDelegate = ExpressionProvider.CreateConstructorDelegate(typeof(ExpressionTestClass));
        Assert.IsNotNull(constructorDelegate);

        var constructorFunc = constructorDelegate as Func<ExpressionTestClass>;
        Assert.IsNotNull(constructorFunc);
        
        Measure.Method(() => _ = (constructorDelegate as Func<ExpressionTestClass>).Invoke()).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(castingGroup).Run();
        Measure.Method(() => _ = constructorFunc.Invoke()).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(directGroup).Run();
        GC.Collect();
    }
}
