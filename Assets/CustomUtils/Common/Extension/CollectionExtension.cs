using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class CollectionExtension {

    #region [Common]

    public static IEnumerator CloneEnumerator(this ICollection collection) => new ArrayList(collection).GetEnumerator();
    public static IEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, bool isAscending) => isAscending ? enumerable.OrderBy(keySelector) : enumerable.OrderByDescending(keySelector) as IEnumerable<TSource>;
    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource> enumerable) where TSource : class => enumerable.Where(source => source != null);
    
    // TODO. Need Test
    public static Dictionary<TKey, TValue> ToDictionaryWithDistinct<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionary<Dictionary<TKey, TValue>, TSource, TKey, TValue>(keySelector, valueSelector);
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionary<ConcurrentDictionary<TKey, TValue>, TSource, TKey, TValue>(keySelector, valueSelector);
    
    public static TDictionary ToDictionary<TDictionary, TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TDictionary : IDictionary<TKey, TValue>, new() {
        var dictionary = new TDictionary();
        foreach (var source in enumerable) {
            dictionary.AutoAdd(keySelector.Invoke(source), valueSelector.Invoke(source));
        }

        return dictionary;
    }

    public static bool TryFirst<T>(this IEnumerable<T> enumerable, out T matchItem, Predicate<T> match = null) {
        if (match == null) {
            matchItem = enumerable.First();
            return matchItem != null;
        }

        foreach (var item in enumerable) {
            if (match?.Invoke(item) ?? false) {
                matchItem = item;
                return true;
            }
        }

        matchItem = default;
        return false;
    }
    
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
        if (enumerable != null) {
            foreach (var item in enumerable) {
                action?.Invoke(item);
            }
        }
    }

    public static bool TryCast<T>(this IEnumerable enumerable, out IEnumerable<T> cast) {
        cast = enumerable?.Cast<T>();
        return cast != null;
    }

    public static bool TryCastList<T>(this IEnumerable enumerable, out List<T> castList) {
        castList = enumerable?.CastList<T>();
        return castList != null;
    }

    public static List<T> CastList<T>(this IEnumerable enumerable) => enumerable?.Cast<T>().ToList();

    public static Vector2 SumVector<T>(this IEnumerable<T> source, Func<T, Vector2> selector) => source.Select(selector).Aggregate((x, y) => x + y);
    public static Vector3 SumVector<T>(this IEnumerable<T> source, Func<T, Vector3> selector) => source.Select(selector).Aggregate((x, y) => x + y);

    public static Vector2 AverageVector<T>(this IEnumerable<T> source, Func<T, Vector2> selector) => source.SumVector(selector) / source.Count();
    public static Vector3 AverageVector<T>(this IEnumerable<T> source, Func<T, Vector3> selector) => source.SumVector(selector) / source.Count();
    
    public static void SafeClear<TValue>(this ICollection<TValue> collection, Action<TValue> releaseAction) {
        try {
            foreach (var value in collection) {
                releaseAction?.Invoke(value);
            }
            
            if (collection.GetType().IsArray == false) {
                collection.Clear();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }

    public static void SafeClear<TValue>(this ICollection<TValue> collection) where TValue : IDisposable {
        try {
            foreach (var value in collection) {
                value.Dispose();
            }

            if (collection.GetType().IsArray == false) {
                collection.Clear();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }

    public static List<T> ToList<T>(this ReadOnlySpan<T> span) {
        var list = new List<T>();
        foreach (var value in span) {
            list.Add(value);
        }

        return list;
    }

    public static TCast[] ToArray<TValue, TCast>(this ReadOnlySpan<TValue> span, Func<TValue, TCast> selector) {
        var array = new TCast[span.Length];
        for (var i = 0; i < array.Length; i++) {
            array[i] = selector.Invoke(span[i]);
        }

        return array;
    }

    #endregion

    #region [Dictionary]

    public static void Sync<TKey, TValue>(this IDictionary<TKey, TValue> workDictionary, ISet<TKey> sourceSet, Func<TKey, TValue> createFunc) {
        var removeSet = new HashSet<TKey>();
        foreach (var key in workDictionary.Keys) {
            if (sourceSet.Contains(key) == false) {
                removeSet.Add(key);
            }
        }

        removeSet.ForEach(key => workDictionary.Remove(key));
        sourceSet.ForEach(key => workDictionary.TryAdd(key, createFunc.Invoke(key)));
    }

    public static void SafeClear<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Action<TKey, TValue> releaseAction) {
        try {
            dictionary.ForEach(x => releaseAction?.Invoke(x.Key, x.Value));
            dictionary.Clear();
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void SafeClear<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Action<TValue> releaseAction) {
        try {
            dictionary.Values.ForEach(x => releaseAction?.Invoke(x));
            dictionary.Clear();
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void SafeClear<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TValue : IDisposable {
        try {
            dictionary.Values.ForEach(x => x.Dispose());
            dictionary.Clear();
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key] = value;
        } else {
            dictionary.Add(key, value);
        }
    }

    public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key].Add(value);
        } else {
            dictionary.Add(key, new List<TValue> { value });
        }
    }

    public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key].Add(value);
        } else {
            dictionary.Add(key, new HashSet<TValue> { value });
        }
    }

    public static void AutoAdd<TKey, TIKey, TValue>(this Dictionary<TKey, Dictionary<TIKey, TValue>> dictionary, TKey outKey, TIKey innerKey, TValue value) {
        if (dictionary.ContainsKey(outKey)) {
            dictionary[outKey].AutoAdd(innerKey, value);
        } else {
            dictionary.Add(outKey, new Dictionary<TIKey, TValue> { { innerKey, value } });
        }
    }

    public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, Queue<TValue>> dictionary, TKey key) {
        if (dictionary.ContainsKey(key) == false) {
            dictionary.Add(key, new Queue<TValue>());
        }
    }

    public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, Queue<TValue>> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key].Enqueue(value);
        } else {
            var queue = new Queue<TValue>();
            queue.Enqueue(value);
            dictionary.Add(key, queue);
        }
    }

    public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> pair) => dictionary.AutoAdd(pair.Key, pair.Value);

    public static bool TryGetOrAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value) where TValue : new() {
        value = dictionary.GetOrAddValue(key);
        return value != null;
    }

    public static TValue GetOrAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new() {
        if (dictionary.TryGetValue(key, out var value) == false) {
            value = new TValue();
            dictionary.Add(key, value);
        }

        return value;
    }

    public static void AutoCountingAdd<TKey, TValue>(this Dictionary<TKey, Dictionary<int, TValue>> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key].Add(dictionary[key].Count + 1, value);
        } else {
            dictionary.Add(key, new Dictionary<int, TValue> { { 1, value } });
        }
    }

    public static void AutoIncreaseAdd<TKey>(this IDictionary<TKey, int> dictionary, TKey key) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key] += 1;
        } else {
            dictionary.Add(key, 1);
        }
    }

    public static void AutoAccumulateAdd<TKey>(this IDictionary<TKey, int> dictionary, TKey key, int value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key] += value;
        } else {
            dictionary.Add(key, value);
        }
    }

    public static void AutoAccumulateAdd<TKey>(this IDictionary<TKey, long> dictionary, TKey key, long value) {
        if (dictionary.ContainsKey(key))
            dictionary[key] += value;
        else
            dictionary.Add(key, value);
    }

    public static void AutoAccumulateAdd<TKey>(this IDictionary<TKey, double> dictionary, TKey key, double value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key] += value;
        } else {
            dictionary.Add(key, value);
        }
    }

    public static void AutoAccumulateAdd<TKey, TIKey>(this IDictionary<TKey, IDictionary<TIKey, int>> dictionary, TKey outKey, TIKey innerKey, int value) {
        if (dictionary.ContainsKey(outKey)) {
            dictionary[outKey].AutoAccumulateAdd(innerKey, value);
        } else {
            dictionary.Add(outKey, new Dictionary<TIKey, int> { { innerKey, value } });
        }
    }

    public static void AutoRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
        if (dictionary.ContainsKey(key)) {
            dictionary.Remove(key);
        }
    }
    
    public static void SafeRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> releaseAction) {
        if (dictionary.TryGetValue(key, out var value)) {
            releaseAction?.Invoke(value);
            dictionary.Remove(key);
        }
    }
    
    public static void SafeRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : IDisposable {
        if (dictionary.TryGetValue(key, out var value)) {
            value.Dispose();
            dictionary.Remove(key);
        }
    }
    
    public static bool TryGetValue<TKey, TIKey, TValue>(this IDictionary<TKey, Dictionary<TIKey, TValue>> dictionary, TKey outKey, TIKey innerKey, out TValue outValue) {
        outValue = default;
        return dictionary.TryGetValue(outKey, out var innerDic) && innerDic.TryGetValue(innerKey, out outValue);
    }
    
    public static bool TryFindValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, out TValue value, Func<TValue, bool> match) {
        value = dictionary.FindValue(match);
        return value != null;
    }
    
    public static TValue FindValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<TValue, bool> match) {
        var pair = dictionary.FirstOrDefault(x => match?.Invoke(x.Value) ?? false);
        return pair.Equals(default(KeyValuePair<TKey, TValue>)) ? default : pair.Value;
    }

    public static bool TryFindAllValueList<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, out List<TValue> list, Func<TValue, bool> match) {
        list = dictionary.FindAllValueList(match);
        return list is { Count: > 0 };
    }

    public static List<TValue> FindAllValueList<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<TValue, bool> match) => dictionary.Where(x => match?.Invoke(x.Value) ?? false).Select(x => x.Value).ToList();

    public static int FindKeyIndex<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
        var index = 0;
        foreach (var keyItem in dictionary.Keys) {
            if (keyItem.Equals(key)) {
                return index;
            }

            index++;
        }

        return -1;
    }
    public static bool TryFindKeyIndex<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out int index) {
        index = FindKeyIndex(dictionary, key);
        return index >= 0;
    }

    public static TKey FindKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Predicate<TValue> match) {
        foreach (var pair in dictionary) {
            if (match?.Invoke(pair.Value) ?? false) {
                return pair.Key;
            }
        }
        
        return default;
    }

    public static List<TValue> ToValueList<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => dictionary.Values.ToList();
    public static List<TKey> ToKeyList<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => dictionary.Keys.ToList();

    public static string ToStringPair<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, string separator = " ") => $"{pair.Key}{separator}{pair.Value}";
    public static string ToStringCollection<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => string.Join(' ', dictionary.Select(x => $"({x.Key} , {x.Value})"));
    
    public static bool TryGetRandom<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, out TKey key, out TValue value) {
        key = dictionary.GetRandomKey();
        if (key == null) {
            value = default;
            return false;
        }
        
        value = dictionary[key];
        return true;
    }

    public static bool TryGetRandomKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, out TKey key) {
        key = dictionary.GetRandomKey();
        return key != null;
    }

    public static TKey GetRandomKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => dictionary.Keys.ToList().Shuffle().FirstOrDefault();

    public static bool TryGetRandomValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, out TValue value) {
        value = dictionary.GetRandomValue();
        return value != null;
    }
    
    public static TValue GetRandomValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => dictionary.Values.ToList().Shuffle().FirstOrDefault();
    
    public static IEnumerable<TValue> ToValues<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) {
        foreach (var (_, value) in enumerable) {
            yield return value;
        }
    }
    
    public static bool IsTrue<TKey>(this IDictionary<TKey, bool> dictionary, TKey key) => dictionary.TryGetValue(key, out var isTrue) && isTrue;
    public static bool IsDictionary(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    public static bool IsEmpty(this IDictionary dictionary) => dictionary.Count <= 0;

    #endregion

    #region [IList]
    
    public static void ISync<T>(this List<T> workList, IList sourceList, Func<T> createFunc) {
        if (workList.Count < sourceList.Count) {
            var syncCount = sourceList.Count - workList.Count;
            for (var i = 0; i < syncCount; i++) {
                var item = createFunc.Invoke();
                if (item != null) {
                    workList.Add(item);
                }
            }
        }
    }

    public static void ISync<T>(this IList workList, List<T> sourceList, Func<T> createFunc) {
        if (workList.Count < sourceList.Count) {
            var syncCount = sourceList.Count - workList.Count;
            for (var i = 0; i < syncCount; i++) {
                var item = createFunc.Invoke();
                if (item != null) {
                    workList.Add(item);
                }
            }
        }
    }

    public static void IndexForEach<TBase>(this IList<TBase> workList, Action<TBase, int> action) {
        for (var i = 0; i < workList.Count; i++) {
            action?.Invoke(workList[i], i);
        }
    }

    public static void ISyncForEach<TBase, TWork>(this IList<TBase> sourceList, IList<TWork> workList, Func<int, TBase, TWork, bool> checkAction, Action<TBase, TWork> dataAction, Action<TWork> clearAction = null) {
        if (sourceList.Count <= 0)
            return;

        var targetIndex = 0;
        while (targetIndex < workList.Count) {
            var baseIndex = 0;
            do {
                if (checkAction?.Invoke(targetIndex, sourceList[baseIndex], workList[targetIndex]) ?? false) {
                    dataAction?.Invoke(sourceList[baseIndex], workList[targetIndex]);
                    break;
                }

                baseIndex++;
            } while (baseIndex < sourceList.Count);

            if (baseIndex >= sourceList.Count) {
                clearAction?.Invoke(workList[targetIndex]);
            }

            targetIndex++;
        }
    }

    public static void ISyncForEach<T>(this IList sourceList, IList<T> workList, Action<object, T> dataAction, Action<T> clearAction = null) {
        if (sourceList.Count < 0) {
            foreach (var work in workList) {
                clearAction?.Invoke(work);
            }
            return;
        }

        for (var i = 0; i < workList.Count; i++) {
            if (i >= sourceList.Count) {
                for (var j = i; j < workList.Count; j++)
                    clearAction?.Invoke(workList[j]);

                return;
            }

            dataAction?.Invoke(sourceList[i], workList[i]);
        }
    }

    public static void RemoveFirst<TValue>(this IList<TValue> list) {
        if (list.Count > 1) {
            list.RemoveAt(0);
        }
    }

    public static void RemoveLast<TValue>(this IList<TValue> list) {
        if (list.Count > 0) {
            list.RemoveAt(list.Count - 1);
        }
    }
    
    #endregion
    
    #region [List]
    
    public static void Sync<T>(this List<T> list, int maxCount, Func<T> createFunc) {
        if (createFunc == null) {
            return;
        }

        while (list.Count < maxCount) {
            list.Add(createFunc.Invoke());
        }
        
        if (list.Count > maxCount) {
            list.RemoveRange(maxCount, list.Count - maxCount);
        }
    }

    public static void Sync<TBase, TWork>(this List<TBase> workList, List<TWork> sourceList, Func<TBase> createFunc) => ISync(workList, sourceList, createFunc);

    public static void ISync<T>(this List<T> workList, IDictionary sourceList, Func<T> createFunc) {
        if (workList.Count > sourceList.Count) {
            var syncCount = sourceList.Count - workList.Count;
            for (var i = 0; i < syncCount; i++) {
                var item = createFunc.Invoke();
                if (item != null) {
                    workList.Add(item);
                }
            }
        }
    }

    public static void Sync<TBase, TWork>(this List<TBase> sourceList, List<TWork> workList, Func<TWork> createFunc, Action<TWork> removeAction) => ISync(sourceList, workList, createFunc, removeAction);

    public static void ISync<TBase, TWork>(this List<TBase> sourceList, List<TWork> workList, Func<TWork> createFunc, Action<TWork> removeAction) {
        var count = sourceList.Count - workList.Count;
        while (count++ < 0) {
            removeAction?.Invoke(workList[0]);
            workList.RemoveAt(0);
        }

        while (count-- > 0) {
            workList.Add(createFunc.Invoke());
        }
    }

    public static void SyncForEach<TBase, TWork>(this List<TBase> sourceList, List<TWork> workList, Action<TBase, TWork> dataAction, Action<TWork> clearAction = null) {
        if (workList == null) {
            Logger.TraceError($"{nameof(workList)} is Null");
            return;
        }
    
        for (var i = 0; i < workList.Count; i++) {
            if (i >= sourceList.Count) {
                for (var j = i; j < workList.Count; j++) {
                    clearAction?.Invoke(workList[j]);
                }
    
                return;
            }
    
            dataAction?.Invoke(sourceList[i], workList[i]);
        }
    }

    public static void LimitedAdd<T>(this List<T> list, T item, int maxCount) {
        if (list.Count >= maxCount) {
            list.RemoveAt(0);
        }
        
        list.Add(item);
    }

    public static void AutoAdd<T>(this List<T> list, Predicate<T> match, T item) {
        if (list.TryFindIndex(out var index, match)) {
            list[index] = item;
        } else {
            list.Add(item);
        }
    }

    public static bool TryFirst<T>(this List<T> baseList, out T value, int index) {
        if (index + 1 > baseList.Count || index < 0) {
            value = default;
            return false;
        }
        
        value = baseList[index];
        return true;
    }

    public static bool TryFirst<T>(this List<T> list, out T value, Predicate<T> match) {
        value = list.Find(match);
        return value != null;
    }

    public static bool TryFindIndex<T>(this List<T> list, out int index, Predicate<T> match) {
        index = list.FindIndex(match);
        return index != -1;
    }
    
    public static List<T> SortList<T>(this List<T> list, Comparison<T> comparison) {
        list?.Sort(comparison);
        return list;
    }
    
    public static bool IsValidIndex<T>(this List<T> list, int index) {
        if (list == null) {
            return false;
        }

        return index > -1 && list.Count > index;
    }

    public static List<TResult> ConvertTo<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> converter) {
        var convertList = new List<TResult>();
        foreach (var item in enumerable) {
            var convertItem = converter.Invoke(item);
            if (convertItem == null) {
                continue;
            }

            convertList.Add(convertItem);
        }

        return convertList;
    }

    public static IEnumerable<TResult> ConvertTo<T, TResult>(this IEnumerable<T> enumerable, Func<T, IEnumerable<TResult>> converter) {
        var list = new List<TResult>();
        foreach (var item in enumerable) {
            var convertItem = converter?.Invoke(item);
            if (convertItem == null) {
                continue;
            }
            
            list.AddRange(convertItem);
        }

        return list;
    }

    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, char separator = ' ') => string.Join(separator, enumerable);
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, string separator) => string.Join(separator, enumerable);
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, Func<T, string> selector, char separator = ' ') => string.Join(separator, enumerable.Select(selector.Invoke));
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, Func<T, string> selector, string separator) => string.Join(separator, enumerable.Select(selector.Invoke));

    // Fisher–Yates shuffle 기반
    private static readonly Random _randomGenerator = new Random();

    public static List<T> Shuffle<T>(this List<T> list, Predicate<T> match) => Shuffle(list).FindAll(match);

    public static List<T> Shuffle<T>(this List<T> list) {
        try {
            var n = list.Count;
            while (n > 1) {
                var k = _randomGenerator.Next(--n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex.Message);
            return new List<T>();
        }

        return list;
    }

    #endregion

    #region [Array]

    public static List<T> ToList<T>(this Array array) {
        var list = new List<T>();
        foreach (var arrayObject in array) {
            if (arrayObject is T castItem) {
                list.Add(castItem);
            }
        }

        return list;
    }

    public static T[] ToArray<T>(this Array array) => array.Cast<T>().ToArray();

    public static Dictionary<T, int> ToIndexDictionary<T>(this T[] array) {
        var dictionary = new Dictionary<T, int>();
        if (array != null) {
            for (var index = 0; index < array.Length; index++) {
                dictionary.AutoAdd(array[index], index);
            }
        }

        return dictionary;
    }

    public static bool TryFindIndex<T>(this T[] array, T value, out int index) {
        if (array != null) {
            for (var i = 0; i < array.Length; i++) {
                if (value?.Equals(array[i]) ?? false) {
                    index = i;
                    return true;
                }
            }
        }
        
        index = -1;
        return false;
    }

    public static bool TryGetRandom<T>(this T[] array, out T value) {
        value = array.GetRandom();
        return value != null;
    }
    
    public static T GetRandom<T>(this T[] array) {
        try {
            if (array is { Length: > 0 }) {
                var randomIndex = UnityEngine.Random.Range(0, array.Length);
                return array[randomIndex];
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return default;
    }

    public static bool IsEmpty<TValue>(this TValue[] array) {
        try {
            return array.Length <= 0;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }

    #endregion

    #region [Queue]

    public static void Clear<T>(this Queue<T> queue, Func<T, bool> match) {
        for (var i = 0; i < queue.Count; i++) {
            if (queue.TryDequeue(out var value) && (match?.Invoke(value) ?? false)) {
                queue.Enqueue(value);
            }
        }
    }

    #endregion

    #region [HashSet]
    
    public static HashSet<T> ToHashSetWithDistinct<T>(this IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null) => enumerable.Distinct().ToHashSet(equalityComparer);
    public static ImmutableHashSet<T> ToImmutableHashSetWithDistinct<T>(this IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null) => enumerable.Distinct().ToImmutableHashSet(equalityComparer);

    #endregion
}