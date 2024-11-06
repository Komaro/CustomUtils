using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[Category(TestConstants.Category.NOTIFY)]
[Category(TestConstants.Category.COVERAGE)]
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
        
        // operator test
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
        Assert.AreEqual(notifyProperty, new NotifyProperty<int>(SECOND_VALUE));
        Assert.AreEqual(notifyProperty.GetHashCode(), SECOND_VALUE.GetHashCode());

        Logger.TraceLog($"Pass {typeof(NotifyProperty<>).Name} coverage test");
    }

    [Test]
    public void NotifyCollectionCoverageTest() {
        var intList = RandomUtil.GetRandoms(20).ToList();
        var collection = new NotifyCollection<int>();
        collection = new NotifyCollection<int>(intList);
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

        evaluateValue = 7777;
        collection[^1] = evaluateValue;
        Assert.AreEqual(collection[^1], evaluateValue);
        
        collection.Clear();
        Assert.IsTrue(collection.Count == 0);

        // exception test
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
        
        Logger.TraceLog($"Pass {typeof(NotifyCollection<>).Name} coverage test");
    }

    [Test]
    public void NotifyRecordCollectionCoverageTest() {
        var testClassList = RandomUtil.GetRandoms(10, _ => new TestClass(RandomUtil.GetRandom(10))).ToList();
        var collection = new NotifyRecordCollection<TestClass>();
        collection = new NotifyRecordCollection<TestClass>(testClassList.ToArray());
        collection = new NotifyRecordCollection<TestClass>(testClassList);

        // operator test
        Assert.IsTrue(collection == new NotifyRecordCollection<TestClass>(testClassList));
        Assert.IsFalse(collection == null);

        var evaluateCollection = new NotifyRecordCollection<TestClass>(RandomUtil.GetRandoms(10, _ => new TestClass(RandomUtil.GetRandom(10))));
        Assert.IsFalse(collection == evaluateCollection);
        Assert.IsTrue(collection != evaluateCollection);
        
        evaluateCollection.Add(new TestClass(RandomUtil.GetRandom(10)));
        Assert.IsTrue(collection != evaluateCollection);

        Assert.IsFalse(collection.Equals(evaluateCollection));
        Assert.AreNotEqual(collection, evaluateCollection);
        Assert.AreNotEqual(collection.GetHashCode(), 0);

        Logger.TraceLog($"Pass {typeof(NotifyRecordCollection<>).Name} coverage test");
    }
    
    [Test]
    public void NotifyListCoverageTest() {
        var intList = RandomUtil.GetRandoms(20, _ => RandomUtil.GetRandom(0, 10000)).ToList();
        var list = new NotifyList<int>();
        list = new NotifyList<int>(10);
        Assert.AreEqual(list.Capacity, 10);

        list.Capacity = 15;
        Assert.AreEqual(list.Capacity, 15);
        
        list = new NotifyList<int>(intList);
        list = new NotifyList<int>(intList.AsEnumerable());
        list = new NotifyList<int>(intList.ToArray());

        var randomIndex = RandomUtil.GetRandom(0, list.Count);
        var randomItem = list[randomIndex];
        list[randomIndex] = randomItem;
        Assert.AreEqual(randomIndex, list.IndexOf(randomItem));
        
        var evaluateValue = 4533;
        list.Insert(list.Count, evaluateValue);
        Assert.AreEqual(list[^1], evaluateValue);
        
        list.RemoveAt(list.Count - 1);
        Assert.AreNotEqual(list[^1], evaluateValue);

        evaluateValue = 999999999;
        list.Add(evaluateValue);
        Assert.AreEqual(list.Last(), evaluateValue);

        list.Remove(evaluateValue);
        Assert.AreNotEqual(list.Last(), evaluateValue);

        evaluateValue = 7777;
        list[^1] = evaluateValue;
        Assert.AreEqual(list[^1], evaluateValue);
        
        list.Clear();
        Assert.IsTrue(list.Count == 0);

        // exception test
        var nullableIntList = intList.ConvertAll(value => (int?) value);
        var nullableList = new NotifyList<int?>(nullableIntList);
        
        Assert.Throws<NullReferenceException>(() => nullableList[^1] = null);
        Assert.Throws<NullReferenceException>(() => _ = nullableList.IndexOf(null));
        Assert.Throws<NullReferenceException>(() => nullableList.Insert(0, null));
        
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableList[int.MaxValue] = null);
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableList.Insert(int.MaxValue, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableList.Insert(int.MinValue, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableList.RemoveAt(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => nullableList.RemoveAt(int.MinValue));

        Logger.TraceLog($"Pass {typeof(NotifyList<>).Name} coverage test");
    }

    [Test]
    public void NotifyRecordListTest() {
        var intList = RandomUtil.GetRandoms(20).ToList();
        var recordList = new NotifyRecordList<int>();
        recordList = new NotifyRecordList<int>(10);
        recordList = new NotifyRecordList<int>(intList);
        recordList = new NotifyRecordList<int>(intList.ToArray());

        var evaluateList = new NotifyRecordList<int>(intList);
        Assert.IsTrue(recordList == evaluateList);
        Assert.IsFalse(recordList == null);
        Assert.IsFalse(recordList == new NotifyRecordList<int>(RandomUtil.GetRandoms(20)));

        evaluateList.Add(0);
        Assert.IsTrue(recordList != evaluateList);
        evaluateList.RemoveLast();
        
        Assert.IsTrue(recordList.Equals(evaluateList));
        Assert.AreNotEqual(recordList.GetHashCode(), 0);
        
        Logger.TraceLog($"Pass {typeof(NotifyRecordList<>).Name} coverage test");
    }

    [Test]
    public void NotifyDictionaryTest() {
        var comparer = new TestIntEqualityComparer();
        var pairDic = RandomUtil.GetRandoms(20, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000))).ToDictionary(pair => pair.Key, pair => pair.Value);
        var dic = new NotifyDictionary<int, int>();
        dic = new NotifyDictionary<int, int>(20, comparer);
        dic = new NotifyDictionary<int, int>(comparer);
        dic = new NotifyDictionary<int, int>(pairDic.ToArray());
        dic = new NotifyDictionary<int, int>(comparer, pairDic.ToArray());
        dic = new NotifyDictionary<int, int>(pairDic, comparer);
        dic = new NotifyDictionary<int, int>(pairDic.AsEnumerable());
        
        dic.Add(50, 5);
        Assert.IsTrue(dic.ContainsKey(50));
        
        dic.Remove(50);
        Assert.IsFalse(dic.ContainsKey(50));
        dic.Remove(int.MaxValue);
        
        Assert.IsTrue(dic.ContainsKey(0));

        dic.TryGetValue(0, out _);

        _ = dic[0];
        dic[0] = 50;
        Assert.AreEqual(dic[0], 50);
        
        dic[9999] = 50;
        Assert.AreEqual(dic[9999], 50);

        var nullableDic = new NotifyDictionary<int, int?>();
        Assert.Throws<NullReferenceException>(() => nullableDic.Add(0, null));
        _ = nullableDic.Keys;
        _ = nullableDic.Values;
        Assert.IsNull(nullableDic.GetSchema());

        var text = XmlUtil.Serialize(nullableDic);
        _ = XmlUtil.DeserializeFromText<NotifyDictionary<int, int?>>(text);

        nullableDic = new NotifyDictionary<int, int?>(pairDic.ToDictionary(pair => pair.Key, pair => (int?) pair.Value));
        text = XmlUtil.Serialize(nullableDic);
        _ = XmlUtil.DeserializeFromText<NotifyDictionary<int, int?>>(text);

        Logger.TraceLog($"Pass {typeof(NotifyDictionary<,>).Name} coverage test");
    }

    [Test]
    public void NotifySortedDictionaryTest() {
        var comparer = new TestIntComparer();
        var pairDic = RandomUtil.GetRandoms(20, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000))).ToDictionary(pair => pair.Key, pair => pair.Value);
        _ = new NotifySortedDictionary<int, int>();
        _ = new NotifySortedDictionary<int, int>(comparer);
        _ = new NotifySortedDictionary<int, int>(comparer, pairDic.ToArray());
        _ = new NotifySortedDictionary<int, int>(pairDic.ToArray());
        _ = new NotifySortedDictionary<int, int>(pairDic, comparer);
        
        Logger.TraceLog($"Pass {typeof(NotifySortedDictionary<,>)} coverage test");
    }
    
    [Test]
    public void NotifyConcurrentDictionaryTest() {
        var comparer = new TestIntEqualityComparer();
        var pairDic = RandomUtil.GetRandoms(20, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000))).ToDictionary(pair => pair.Key, pair => pair.Value);
        _ = new NotifyConcurrentDictionary<int, int>();
        _ = new NotifyConcurrentDictionary<int, int>(comparer);
        _ = new NotifyConcurrentDictionary<int, int>(pairDic, comparer);
        
        Logger.TraceLog($"Pass {typeof(NotifyConcurrentDictionary<,>)} coverage test");
    }
    
    [Test]
    public void NotifyRecordDictionaryTest() {
        var pairDic = RandomUtil.GetRandoms(20, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000))).ToDictionary(pair => pair.Key, pair => pair.Value);
        var dic = new NotifyRecordDictionary<int, int>();
        dic = new NotifyRecordDictionary<int, int>(pairDic.ToArray());
        dic = new NotifyRecordDictionary<int, int>(pairDic);
        dic = new NotifyRecordDictionary<int, int>(pairDic.AsEnumerable());
        
        var evaluateDic = new NotifyRecordDictionary<int, int>(pairDic);
        Assert.IsTrue(dic == evaluateDic);
        Assert.IsFalse(dic == null);
        Assert.IsFalse(dic == new NotifyRecordDictionary<int, int>(RandomUtil.GetRandoms(20, index => new KeyValuePair<int, int>(index, RandomUtil.GetRandom(0, 10000)))));

        evaluateDic.Add(int.MaxValue, int.MinValue);
        Assert.IsTrue(dic != evaluateDic);
        evaluateDic.Remove(int.MaxValue);
        
        Assert.IsTrue(dic.Equals(evaluateDic));
        Assert.AreNotEqual(dic.GetHashCode(), 0);
        
        Logger.TraceLog($"Pass {typeof(NotifyRecordDictionary<,>)} coverage test");
    }
}

internal sealed class TestIntComparer : Comparer<int> {

    public override int Compare(int x, int y) => x.CompareTo(y);
}

internal sealed class TestIntEqualityComparer : EqualityComparer<int> {

    public override bool Equals(int x, int y) => x.Equals(y);
    public override int GetHashCode(int obj) => obj.GetHashCode();
}