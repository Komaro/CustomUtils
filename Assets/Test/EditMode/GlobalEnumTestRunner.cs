using System;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

public class GlobalEnumTestRunner {

    [SetUp]
    public void SetUp() {
        LogAssert.ignoreFailingMessages = true;
    }

    [TearDown]
    public void TearDown() {
        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void StartTest() {
        var globalEnum = new GlobalEnum<SoundTrackEnumAttribute>();
        
        // Basic Test
        Assert.IsTrue(globalEnum[0] != null);
        foreach (var enumValue in globalEnum) {
            Assert.IsTrue(enumValue != null);
        }
        Logger.TraceLog("Pass Basic Test");
        
        // int Interaction Test
        for (var index = 0; index < globalEnum.Count + 5; index++) {
            if (index < globalEnum.Count) {
                Assert.IsTrue(globalEnum[index] != null);
            } else {
                Assert.IsTrue(globalEnum[index] == null);
            }
        }
        Logger.TraceLog("Pass int Interaction Test");
        
        // Type Interaction Test
        var values = globalEnum[typeof(TEST_GLOBAL_ENUM_01)];
        Assert.IsTrue(values != ImmutableHashSet<Enum>.Empty);

        values = globalEnum[typeof(TEST_GLOBAL_ENUM_02)];
        Assert.IsTrue(values != ImmutableHashSet<Enum>.Empty);

        Logger.TraceLog("Pass Type Interaction Test");
        
        // Property Get Set Test
        var comparisonValue = globalEnum.Value;
        globalEnum.Value = TEST_GLOBAL_ENUM_02.TEST_02;
        Assert.AreNotEqual(comparisonValue, globalEnum.Value);
        Logger.TraceLog("Pass Property Get Set Test");
        
        // Method Get Test
        Assert.IsTrue(globalEnum.Get<TEST_GLOBAL_ENUM_02>() == TEST_GLOBAL_ENUM_02.TEST_02);
        Logger.TraceLog("Pass Method Get Test");

        // Method Set Test
        comparisonValue = globalEnum.Value;
        globalEnum.Set(TEST_GLOBAL_ENUM_01.TEST_03);
        Assert.AreNotEqual(comparisonValue, globalEnum.Value);
        Logger.TraceLog("Pass Method Set Test");
        
        // Contains Test
        foreach (var value in EnumUtil.AsSpan<TEST_GLOBAL_ENUM_01>()) {
            Assert.IsTrue(globalEnum.Contains(value));
        }
        
        foreach (var value in EnumUtil.AsSpan<TEST_GLOBAL_ENUM_02>()) {
            Assert.IsTrue(globalEnum.Contains(value));
        }

        Logger.TraceLog("Pass Contains Test");
    }

    // GlobalEnum 캐싱 규격화 이후 단위 테스트 진행
    [Performance]
    [TestCase(10, 2000)]
    public void GetPerformanceTest(int measurementCount, int count) {
        var unsafeGroup = new SampleGroup("Unsafe", SampleUnit.Microsecond);
        var beforeGroup = new SampleGroup("Before", SampleUnit.Microsecond);
        var afterGroup = new SampleGroup("After", SampleUnit.Microsecond);
        var afterCacheGroup = new SampleGroup("AfterCache", SampleUnit.Microsecond);
        
        var globalEnum = new GlobalEnum<SoundTrackEnumAttribute> {
            Index = 2
        };

        // Measure.Method(() => _ = globalEnum.Get_Unsafe<TEST_GLOBAL_ENUM_01>()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(unsafeGroup).GC().Run();
        // Measure.Method(() => _ = globalEnum.Get_Before<TEST_GLOBAL_ENUM_01>()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(beforeGroup).GC().Run();
        // Measure.Method(() => _ = globalEnum.Get_After<TEST_GLOBAL_ENUM_01>()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(afterGroup).GC().Run();
        // Measure.Method(() => _ = globalEnum.Get_CacheAfter<TEST_GLOBAL_ENUM_01>()).WarmupCount(1).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(afterCacheGroup).GC().Run();
    } 
}

[SoundTrackEnum(priority = 25)]
public enum TEST_GLOBAL_ENUM_01 {
    TEST_01,
    TEST_02,
    TEST_03,
}

[SoundTrackEnum(priority = 10)]
public enum TEST_GLOBAL_ENUM_02 {
    TEST_01,
    TEST_02,
    TEST_03,
}