using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private class StringComparer : IEqualityComparer<string> {

        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode(string obj) => obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
