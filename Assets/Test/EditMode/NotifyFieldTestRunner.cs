using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("Notify")]
public class NotifyCoverageTestRunner {
    
    [Test]
    public void NotifyPropertyCoverageTest() {
        const int FIRST_VALUE = 15; 
        const int SECOND_VALUE = 30;
        var equalityComparer = new TestIntEqualityComparer();
        var notifyProperty = new NotifyProperty<int>();
        Assert.AreSame(notifyProperty.EqualityComparer, EqualityComparer<int>.Default);
        
        notifyProperty = new NotifyProperty<int>(FIRST_VALUE, equalityComparer);
        Assert.AreSame(notifyProperty.EqualityComparer, equalityComparer);
        
        notifyProperty = new NotifyProperty<int>(equalityComparer);
        Assert.AreSame(notifyProperty.EqualityComparer, equalityComparer);
        
        notifyProperty = new NotifyProperty<int>(FIRST_VALUE);
        Assert.AreSame(notifyProperty.EqualityComparer, EqualityComparer<int>.Default);

        notifyProperty.EqualityComparer = equalityComparer;
        Assert.AreSame(notifyProperty.EqualityComparer, equalityComparer);

        notifyProperty.EqualityComparer = EqualityComparer<int>.Default;
        Assert.AreSame(notifyProperty.EqualityComparer, EqualityComparer<int>.Default);
        
        var getValue = notifyProperty.Value;
        getValue = notifyProperty;
        Assert.IsTrue(getValue == FIRST_VALUE);
        notifyProperty.Value = FIRST_VALUE;
        notifyProperty.Value = SECOND_VALUE;
        
        Assert.IsTrue(notifyProperty == SECOND_VALUE);
        Assert.IsFalse(notifyProperty == FIRST_VALUE);
        
        Assert.IsTrue(notifyProperty != FIRST_VALUE);
        Assert.IsFalse(notifyProperty != SECOND_VALUE);
        
        Assert.IsTrue(notifyProperty == new NotifyProperty<int>(SECOND_VALUE));
        Assert.IsFalse(notifyProperty == new NotifyProperty<int>(FIRST_VALUE));
        
        Assert.IsTrue(notifyProperty != new NotifyProperty<int>(FIRST_VALUE));
        Assert.IsFalse(notifyProperty != new NotifyProperty<int>(SECOND_VALUE));
        
        Assert.IsFalse(notifyProperty == null);
        Assert.IsTrue(notifyProperty != null);
        
        Assert.IsTrue(notifyProperty == notifyProperty);
        Assert.IsFalse(notifyProperty != notifyProperty);

        Assert.IsNotEmpty(notifyProperty.ToString());
        Assert.IsTrue(notifyProperty.Equals(new NotifyProperty<int>(SECOND_VALUE)));
        Assert.IsTrue(notifyProperty.GetHashCode() == SECOND_VALUE.GetHashCode());
        
        Logger.TraceLog($"Pass {typeof(NotifyProperty<>).Name} test");
    }

    private sealed class TestIntEqualityComparer : EqualityComparer<int> {

        public override bool Equals(int x, int y) => x.Equals(y);
        public override int GetHashCode(int obj) => obj.GetHashCode();
    }

    [Test]
    public void NotifyCollectionCoverageTest() {
        var intList = RandomUtil.GetRandoms(20).ToList();
        var collection = new NotifyCollection<int>();
        collection = new NotifyCollection<int>(intList);
        collection = new NotifyCollection<int>(intList.AsEnumerable());
        collection = new NotifyCollection<int>(intList.ToArray());

        var randomIndex = RandomUtil.GetRandom(0, intList.Count);
        var randomItem = intList[randomIndex];
        Assert.AreEqual(randomIndex, collection.IndexOf(randomItem));

        var evaluateValue = 4533;
        collection.Insert(collection.Count, evaluateValue);
        Assert.AreEqual(collection[^1], evaluateValue);
        
        collection.RemoveAt(collection.Count - 1);
        Assert.AreNotEqual(collection[^1], evaluateValue);

        evaluateValue = 999999999;
        collection.Add(evaluateValue);
        Assert.AreEqual(collection.Last(), evaluateValue);

        collection.Remove(evaluateValue);
        Assert.AreNotEqual(collection.Last(), evaluateValue);
        
        collection.Clear();
        Assert.IsTrue(collection.Count == 0);

        // Exception Test
        var nullableIntList = intList.ConvertAll(value => (int?) value);
        var nullableCollection = new NotifyCollection<int?>(nullableIntList);
        Assert.Throws<NullReferenceException>( () => nullableCollection.Add(null));
        Assert.Throws<NullReferenceException>(() => _ = nullableCollection.IndexOf(null));
        Assert.Throws<NullReferenceException>(() => nullableCollection.Insert(0, null));

        Assert.Throws<InvalidCastException>(() => nullableCollection.CopyTo(new float[10], 5));

        Assert.Throws<ArgumentOutOfRangeException>(() => nullableCollection.Insert(999999, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableCollection.Insert(-999999, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableCollection.RemoveAt(99999));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableCollection.RemoveAt(-99999));
        
        Logger.TraceLog($"Pass {typeof(NotifyCollection<>).Name} test");
    }
}

[Category("Notify")]
public class NotifyConstructorTestRunner {

    [Test(Description = "NotifyCollection constructor test")]
    public void NotifyCollectionAllConstructorTest() {
        var intList = RandomUtil.GetRandoms(10).ToList();
        var notifyCollection = new NotifyCollection<int>();
        Assert.IsNotNull(notifyCollection);
        EvaluateCollection(notifyCollection = new NotifyCollection<int>(intList.AsEnumerable()), intList);
        EvaluateCollection(notifyCollection = new NotifyCollection<int>(intList), intList);
        EvaluateCollection(notifyCollection = new NotifyCollection<int>(intList.ToArray()), intList);
        PrintPassResult(typeof(NotifyCollection<int>), notifyCollection, intList);

        intList = RandomUtil.GetRandoms(10).ToList();
        notifyCollection = new NotifyRecordCollection<int>();
        Assert.IsNotNull(notifyCollection);
        EvaluateCollection(notifyCollection = new NotifyRecordCollection<int>(intList.AsEnumerable()), intList);
        EvaluateCollection(notifyCollection = new NotifyRecordCollection<int>(intList.ToArray()), intList);
        PrintPassResult(typeof(NotifyRecordCollection<int>), notifyCollection, intList);
    }

    [Test(Description = "NotifyList constructor test")]
    public void NotifyListAllConstructorTest() {
        var intList = RandomUtil.GetRandoms(10).ToList();
        var notifyList = new NotifyList<int>();
        Assert.IsNotNull(notifyList);
        Assert.IsNotNull(notifyList = new NotifyList<int>(30));
        Assert.IsTrue(notifyList.Capacity == 30);
        EvaluateCollection(notifyList = new NotifyList<int>(intList.AsEnumerable()), intList);
        EvaluateCollection(notifyList = new NotifyList<int>(intList.ToArray()), intList);
        PrintPassResult(typeof(NotifyList<int>), notifyList, intList);

        notifyList = new NotifyRecordList<int>();
        Assert.IsNotNull(notifyList);
        Assert.IsNotNull(notifyList = new NotifyRecordList<int>(99));
        Assert.IsTrue(notifyList.Capacity == 99);
        EvaluateCollection(notifyList = new NotifyRecordList<int>(intList.AsEnumerable()), intList);
        EvaluateCollection(notifyList = new NotifyRecordList<int>(intList.ToArray()), intList);
        PrintPassResult(typeof(NotifyRecordList<int>), notifyList, intList); 
    }

    [Test(Description = "NotifyDictionary constructor test")]
    public void NotifyDictionaryConstructorTest() {
        var pairDic = RandomUtil.GetRandoms(10, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000))).ToDictionary(pair => pair.Key, pair => pair.Value);
        var equalityComparer = EqualityComparer<int>.Default;
        var notifyDic = new NotifyDictionary<int, int>();
        Assert.IsNotNull(notifyDic);
        Assert.IsNotNull(notifyDic = new NotifyDictionary<int, int>(10, equalityComparer));
        Assert.IsNotNull(notifyDic = new NotifyDictionary<int, int>(equalityComparer));
        EvaluateCollection(notifyDic = new NotifyDictionary<int, int>(pairDic.ToArray()), pairDic);
        EvaluateCollection(notifyDic = new NotifyDictionary<int, int>(equalityComparer, pairDic.ToArray()), pairDic);
        EvaluateCollection(notifyDic = new NotifyDictionary<int, int>(pairDic, equalityComparer), pairDic);
        EvaluateCollection(notifyDic = new NotifyDictionary<int, int>(pairDic.AsEnumerable(), equalityComparer), pairDic);
        PrintPassResult(typeof(NotifyDictionary<int, int>), notifyDic, pairDic);

        var notifySortedDic = new NotifySortedDictionary<int, int>();
        var comparer = Comparer<int>.Default;
        Assert.IsNotNull(notifySortedDic);
        Assert.IsNotNull(notifySortedDic = new NotifySortedDictionary<int, int>(comparer));
        EvaluateCollection(notifySortedDic = new NotifySortedDictionary<int, int>(comparer, pairDic.ToArray()), pairDic);
        EvaluateCollection(notifySortedDic = new NotifySortedDictionary<int, int>(pairDic.ToArray()), pairDic);
        EvaluateCollection(notifySortedDic = new NotifySortedDictionary<int, int>(pairDic, comparer), pairDic);
        PrintPassResult(typeof(NotifySortedDictionary<int, int>), notifySortedDic, pairDic);

        var notifyConcurrentDic = new NotifyConcurrentDictionary<int, int>();
        Assert.IsNotNull(notifyConcurrentDic);
        Assert.IsNotNull(notifyConcurrentDic = new NotifyConcurrentDictionary<int, int>(equalityComparer));
        EvaluateCollection(notifyConcurrentDic = new NotifyConcurrentDictionary<int, int>(pairDic.AsEnumerable(), equalityComparer), pairDic);
        PrintPassResult(typeof(NotifyConcurrentDictionary<int, int>), notifyConcurrentDic, pairDic);

        var notifyRecordDic = new NotifyRecordDictionary<int, int>();
        Assert.IsNotNull(notifyRecordDic);
        EvaluateCollection(notifyRecordDic = new NotifyRecordDictionary<int, int>(pairDic.ToArray()), pairDic);
        EvaluateCollection(notifyRecordDic = new NotifyRecordDictionary<int, int>(pairDic), pairDic);
        EvaluateCollection(notifyRecordDic = new NotifyRecordDictionary<int, int>(pairDic.AsEnumerable()), pairDic);
        PrintPassResult(typeof(NotifyRecordDictionary<int, int>), notifyRecordDic, pairDic);
    }

    private void EvaluateCollection<TCollection, TValue>(NotifyCollection<TCollection, TValue> collection, IEnumerable<TValue> list) where TCollection : ICollection<TValue>, new() {
        Assert.IsNotNull(collection, $"{typeof(TCollection).Name} type {nameof(collection)} is null");
        Assert.IsTrue(collection == list, $"\n{collection.ToStringCollection(", ")}\n{list.ToStringCollection(", ")}\n");
    }

    private void PrintPassResult<TCollection, TValue>(Type type, NotifyCollection<TCollection, TValue> collection, IEnumerable<TValue> list) where TCollection : ICollection<TValue>, new() {
        Logger.TraceLog($"Pass {type.Name} constructor test");
        Logger.TraceLog($"\n{collection.ToStringCollection(", ")}\n{list.ToStringCollection(", ")}\n");
    }
}

[Category("Notify")]
public class NotifySerializeTestRunner {

    private NotifyProperty<int> _field;
    private NotifyConvertTestSample _sample;
    private NotifyComplexConvertTestSample _complexSample;

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _field = new NotifyProperty<int> { Value = 4533 };

        _sample = new NotifyConvertTestSample {
            intProperty = { Value = RandomUtil.GetRandom(0, 10000) },
            floatProperty = { Value = RandomUtil.GetRandom(0, 10000) },
            stringProperty = { Value = RandomUtil.GetRandom(20) },
        };

        _complexSample = new NotifyComplexConvertTestSample();
        _complexSample.intProperty.Value = RandomUtil.GetRandom(0, 10000);
        _complexSample.floatProperty.Value = RandomUtil.GetRandom(0, 10000);
        _complexSample.stringProperty.Value = RandomUtil.GetRandom(20);
        _complexSample.intCollection = new NotifyRecordCollection<int>(RandomUtil.GetRandoms(5));
        _complexSample.stringList = new NotifyRecordList<string>(RandomUtil.GetRandoms(10, _ => RandomUtil.GetRandom(10)));
        _complexSample.recordDictionary = new NotifyRecordDictionary<int, float>(RandomUtil.GetRandoms(10, index => new KeyValuePair<int, float>(index, RandomUtil.GetRandom(0.0f, 1000.0f))));
        _complexSample.structDictionary = new NotifyRecordDictionary<int, TestStruct>(RandomUtil.GetRandoms(10, index => new KeyValuePair<int, TestStruct>(index, new TestStruct(RandomUtil.GetRandom(20)))));
    }
    
    [Test(Description = "Notify Csv Convert Test")]
    public void NotifyCsvConvertTest() {
        var text = CsvUtil.Serialize(new[] { _field });
        Assert.IsNotNull(text);
        Logger.TraceLog($"\n{text.TrimEnd()}");
        Logger.TraceLog("Pass field csv serialize");

        var deserializeIntField = CsvUtil.DeserializeFromText<NotifyProperty<int>>(text).First();
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} csv convert test\n");
        Logger.TraceLog($"\n{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = CsvUtil.Serialize<NotifyPropertyClassMap<NotifyConvertTestSample>>(new[] { _sample });
        Assert.IsNotEmpty(text);
        Logger.TraceLog($"\n{text.TrimEnd()}");
        
        var deserializeSample = CsvUtil.DeserializeFromText<NotifyConvertTestSample, NotifyPropertyClassMap<NotifyConvertTestSample>>(text).First();
        Assert.IsNotNull(deserializeSample);
        EvaluateNotifyConvertTestSample(deserializeSample);
        Logger.TraceLog($"Pass {nameof(NotifyConvertTestSample)} csv convert test");
        Logger.TraceLog($"\n{deserializeSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");
    }

    [Test(Description = "Notify Json Convert Test")]
    public void NotifyJsonConvertTest() {
        var text = JsonConvert.SerializeObject(_field, Formatting.Indented);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field json serialize");
        
        var deserializeIntProperty = JsonConvert.DeserializeObject<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntProperty);
        Logger.TraceLog($"Pass {nameof(NotifyField)} json convert test\n");
        Logger.TraceLog($"{deserializeIntProperty.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = JsonConvert.SerializeObject(_complexSample, Formatting.Indented);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample json serialize");

        var deserializeTestSample = JsonConvert.DeserializeObject<NotifyComplexConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyComplexConvertTestSample)} json convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    [Test(Description = "Notify Xml Convert Test")]
    public void NotifyXmlConvertTest() {
        var text = XmlUtil.Serialize(_field);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field xml serialize");
        
        var deserializeIntField = XmlUtil.DeserializeAsClassFromText<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} xml convert test\n");
        Logger.TraceLog($"{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = XmlUtil.Serialize(_complexSample);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample xml serialize");

        var deserializeTestSample = XmlUtil.DeserializeAsClassFromText<NotifyComplexConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyComplexConvertTestSample)} xml convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    private void EvaluateNotifyProperty(NotifyProperty<int> property) {
        Assert.IsNotNull(property, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} null check");

        Assert.IsTrue(ReferenceEquals(_field, property) == false, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} reference check");
        
        Assert.IsTrue(_field == property, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} value check");
    }

    private void EvaluateNotifyConvertTestSample(NotifyConvertTestSample evaluateSample) {
        Assert.IsNotNull(evaluateSample, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} null check");

        var comparisonSample = evaluateSample is NotifyComplexConvertTestSample ? _complexSample : _sample;
        Assert.IsTrue(ReferenceEquals(comparisonSample, evaluateSample) == false, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} reference check");

        Assert.IsTrue(comparisonSample == evaluateSample, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} value check");
    }

    public record NotifyConvertTestSample {
        
        public NotifyProperty<int> intProperty = new();
        public NotifyProperty<float> floatProperty = new();
        public NotifyProperty<string> stringProperty = new();
    }

    public record NotifyComplexConvertTestSample : NotifyConvertTestSample {

        public NotifyRecordCollection<int> intCollection;
        public NotifyRecordList<string> stringList;
        public NotifyRecordDictionary<int, float> recordDictionary;
        public NotifyRecordDictionary<int, TestStruct> structDictionary;
        public NotifyRecordDictionary<int, TestClass> classDictionary = new();
    }

    public class TestClass {

        public string text;
        
        public TestClass() { }
        public TestClass(string text) => this.text = text;
    }

    public struct TestStruct {
        
        public string text;

        public TestStruct(string text) => this.text = text;
    }
}
