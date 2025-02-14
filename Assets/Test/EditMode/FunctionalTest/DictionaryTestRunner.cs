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
            var innerKey = RandomUtil.GetRandom(10);
            for (var j = 0; j < innerCapacity; j++) {
                originDic.AutoAdd(outKey, innerKey, new MultiLevelTestClass());
            }
        }

        var valueDic = new MultiLevelDictionary<int, long, int>();
        Assert.NotNull(valueDic);
        for (var i = 0; i < outCapacity; i++) {
            valueDic.Add(RandomUtil.GetRandom(0, 10000), RandomUtil.GetRandom(0, 100000), RandomUtil.GetRandom(0, 100000));
        }

        var structDic = new MultiLevelDictionary<string, int, MultiLevelTestStruct>();
        Assert.NotNull(structDic);
        for (var i = 0; i < outCapacity; i++) {
            structDic.Add(RandomUtil.GetRandom(10), RandomUtil.GetRandom(0, 100000), new MultiLevelTestStruct(RandomUtil.GetRandom(0, 10000)));
        }

        var classDic = new MultiLevelDictionary<int, string, MultiLevelTestClass>();
        Assert.NotNull(classDic);
        for (var i = 0; i < outCapacity; i++) {
            classDic.Add(RandomUtil.GetRandom(0, 10000), RandomUtil.GetRandom(10), new MultiLevelTestClass());
        }
    }

    private struct MultiLevelTestStruct {
        
        public int testInt;

        public MultiLevelTestStruct(int testInt) => this.testInt = testInt;
    }
    
    private class MultiLevelTestClass {
        
        public int testInt = RandomUtil.GetRandom(0, 10000);
    }
    
    private class StringComparer : IEqualityComparer<string> {

        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode(string obj) => obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
