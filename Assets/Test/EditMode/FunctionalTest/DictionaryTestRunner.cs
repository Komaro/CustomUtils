using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
public class DictionaryTestRunner {

    private ConcurrentDictionary<string, string> _dictionary = new(new StringComparer());

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        
    }

    [TearDown]
    public void TearDown() {
        _dictionary.Clear();
    }
    
    [Test]
    public void DictionaryComparerTest() {
        foreach (var text in RandomUtil.GetRandoms(20, _ => RandomUtil.GetRandom(10))) {
            var upperText = text.ToUpper();
            _dictionary.TryAdd(text, upperText);
            Assert.IsTrue(text.GetHashCode(StringComparison.OrdinalIgnoreCase) == upperText.GetHashCode(StringComparison.OrdinalIgnoreCase));
            if (_dictionary.TryGetValue(upperText, out var value)) {
                Logger.TraceLog($"{text} || {upperText} || {value}");
            }
        }
    }

    [TestCase(10, 10)]
    [TestCase(10, 50)]
    public void MultiLayerDictionaryTest(int outCapacity, int innerCapacity) {
        var originDic = new Dictionary<int, Dictionary<string, MultiLevelTestClass>>();
        for (var i = 0; i < outCapacity; i++) {
            var outKey = RandomUtil.GetRandom(0, 100000);
            for (var j = 0; j < innerCapacity; j++) {
                originDic.AutoAdd(outKey, RandomUtil.GetRandom(10), new MultiLevelTestClass());
            }
        }

        var multiLayerDic = new MultiLayerDictionary<int, string, MultiLevelTestClass>(originDic);
        Assert.NotNull(multiLayerDic);
        foreach (var pair in originDic) {
            Assert.IsTrue(multiLayerDic.TryGetValue(pair.Key, out var innerDic));
            foreach (var (key, value) in pair.Value) {
                Assert.IsTrue(innerDic.TryGetValue(key, out var innerValue));
                Assert.IsTrue(value == innerValue);
            }
        }
        
        var concurrentMultiLayerDic = new MultiLayerConcurrentDictionary<int, string, MultiLevelTestClass>(originDic);
        Assert.NotNull(concurrentMultiLayerDic);
        foreach (var pair in originDic) {
            Assert.IsTrue(concurrentMultiLayerDic.TryGetValue(pair.Key, out var innerDic));
            foreach (var (key, value) in pair.Value) {
                Assert.IsTrue(innerDic.TryGetValue(key, out var innerValue));
                Assert.IsTrue(value == innerValue);
            }
        }
    }

    [TestCase(10, 10)]
    [TestCase(10, 50)]
    public void ListDictionaryTest(int outCapacity, int innerCapacity) {
        var originListDic = new Dictionary<int, List<MultiLevelTestClass>>();
        for (var i = 0; i < outCapacity; i++) {
            var outKey = RandomUtil.GetRandom(0, 100000);
            originListDic.Add(outKey, new List<MultiLevelTestClass>());
            for (var j = 0; j < innerCapacity; j++) {
                originListDic[outKey].Add(new MultiLevelTestClass());
            }
        }
        
        var listDic = new ListDictionary<int, MultiLevelTestClass>(originListDic);
        Assert.NotNull(listDic);
        foreach (var pair in originListDic) {
            Assert.IsTrue(listDic.TryGetValue(pair.Key, out var list));
            Assert.IsTrue(list.SequenceEqual(pair.Value));
        }

        var randomInt = RandomUtil.GetRandom(1, 10000);
        listDic.Add(randomInt, new MultiLevelTestClass());
        Assert.IsTrue(listDic.TryGetValue(randomInt, out var randomList));
        Assert.IsTrue(listDic.TryGetValue(randomInt, 0, out var randomClass));
        Assert.IsTrue(listDic.TryGetValue(out randomClass, randomInt, value => value.testInt == randomClass.testInt));
        
        Assert.IsFalse(listDic.TryGetValue(randomInt - 1, out randomList));
        Assert.IsFalse(listDic.TryGetValue(randomInt - 1, 0, out randomClass));
        Assert.IsFalse(listDic.TryGetValue(randomInt - 1, 1, out randomClass));
        Assert.IsFalse(listDic.TryGetValue(out randomClass, randomInt, value => value.testInt == 0));
        Assert.IsFalse(listDic.TryGetValue(out randomClass, randomInt - 1, value => value.testInt == 0));
    }

    [Performance]
    [TestCase(10, 10)]
    public void DictionaryLoopTest(int measurementCount, int count) {
        var lookUpSample = new SampleGroup("LookUp", SampleUnit.Microsecond);
        var lookUpOptimizeSample = new SampleGroup("LookUpOptimize", SampleUnit.Microsecond);
        var entrySample = new SampleGroup("Entry", SampleUnit.Microsecond);
        var valueCollectionSample = new SampleGroup("ValueCollection", SampleUnit.Microsecond);
        
        var dictionary = new Dictionary<int, int>();
        foreach (var random in RandomUtil.GetRandoms(1000)) {
            dictionary.TryAdd(random, RandomUtil.GetRandom(1, 999999));
        }
        
        Measure.Method(() => {
            var keys = dictionary.Keys.ToList();
            for (var i = 0; i < keys.Count; i++) {
                _ = dictionary[i];
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(lookUpSample).GC().Run();
        
        Measure.Method(() => {
            foreach (var key in dictionary.Keys) {
                _ = dictionary[key];
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(lookUpOptimizeSample).GC().Run();
        
        Measure.Method(() => {
            foreach (var (_, value) in dictionary) {
                _ = value;
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(entrySample).GC().Run();
        
        Measure.Method(() => {
            foreach (var value in dictionary.Values) {
                _ = value;
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(valueCollectionSample).GC().Run();
    }

    private class MultiLevelTestClass {
        
        public int testInt = RandomUtil.GetRandom(0, 10000);
    }
    
    private class StringComparer : IEqualityComparer<string> {

        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode(string obj) => obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
