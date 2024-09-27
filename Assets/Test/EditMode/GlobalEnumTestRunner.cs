using System;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
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
        
        // Count Test
        var totalEnumCount = EnumUtil.GetValues<TEST_GLOBAL_ENUM_01>().Count() + EnumUtil.GetValues<TEST_GLOBAL_ENUM_02>().Count();
        Assert.IsTrue(globalEnum.Count == totalEnumCount);
        Logger.TraceLog("Pass Count Test");
        
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
        foreach (var value in EnumUtil.GetValues<TEST_GLOBAL_ENUM_01>()) {
            Assert.IsTrue(globalEnum.Contains(value));
        }
        
        foreach (var value in EnumUtil.GetValues<TEST_GLOBAL_ENUM_02>()) {
            Assert.IsTrue(globalEnum.Contains(value));
        }

        Logger.TraceLog("Pass Contains Test");
    }
}