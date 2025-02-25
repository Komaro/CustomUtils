using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

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
    public void MultiLevelDictionaryTest(int outCapacity, int innerCapacity) {
        var originDic = new Dictionary<int, Dictionary<string, MultiLevelTestClass>>();
        for (var i = 0; i < outCapacity; i++) {
            var outKey = RandomUtil.GetRandom(0, 100000);
            for (var j = 0; j < innerCapacity; j++) {
                originDic.AutoAdd(outKey, RandomUtil.GetRandom(10), new MultiLevelTestClass());
            }
        }

        var multiLevelDic = new MultiLevelDictionary<int, string, MultiLevelTestClass>(originDic);
        Assert.NotNull(multiLevelDic);
        foreach (var pair in originDic) {
            Assert.IsTrue(multiLevelDic.TryGetValue(pair.Key, out var innerDic));
            foreach (var (key, value) in pair.Value) {
                Assert.IsTrue(innerDic.TryGetValue(key, out var innerValue));
                Assert.IsTrue(value == innerValue);
            }
        }
        
        var concurrentMultiLevelDic = new ConcurrentMultiLevelDictionary<int, string, MultiLevelTestClass>(originDic);
        Assert.NotNull(concurrentMultiLevelDic);
        foreach (var pair in originDic) {
            Assert.IsTrue(concurrentMultiLevelDic.TryGetValue(pair.Key, out var innerDic));
            foreach (var (key, value) in pair.Value) {
                Assert.IsTrue(innerDic.TryGetValue(key, out var innerValue));
                Assert.IsTrue(value == innerValue);
            }
        }
    }

    private class MultiLevelTestClass {
        
        public int testInt = RandomUtil.GetRandom(0, 10000);
    }
    
    private class StringComparer : IEqualityComparer<string> {

        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode(string obj) => obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
