using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[Category(TestConstants.Category.NOTIFY)]
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