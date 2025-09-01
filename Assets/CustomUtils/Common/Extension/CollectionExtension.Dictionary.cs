using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static partial class CollectionExtension {

    public static IEnumerable<TValue> Search<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
        dictionary.ThrowIfNull(nameof(dictionary));
        if (dictionary.TryGetValue(key, out var value)) {
            yield return value;
        }
    }

    public static void Sync<TKey, TValue>(this IDictionary<TKey, TValue> workDictionary, ISet<TKey> sourceSet, Func<TKey, TValue> creator) {
        var removeSet = new HashSet<TKey>();
        foreach (var key in workDictionary.Keys) {
            if (sourceSet.Contains(key) == false) {
                removeSet.Add(key);
            }
        }

        removeSet.ForEach(key => workDictionary.Remove((TKey) key));
        sourceSet.ForEach(key => workDictionary.TryAdd<TKey, TValue>(key, creator.Invoke(key)));
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IDictionary<TKey, TCollection> AutoAdd<TCollection, TKey, TValue>(this IDictionary<TKey, TCollection> dictionary, TKey key) where TCollection : IEnumerable<TValue>, new() {
        if (dictionary.ContainsKey(key) == false) {
            dictionary.Add(key, new TCollection());
        }

        return dictionary;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IDictionary<TKey, TDictionary> AutoAdd<TDictionary, TKey, TIKey, TValue>(this IDictionary<TKey, TDictionary> dictionary, TKey key) where TDictionary : IDictionary<TIKey, TValue>, new() {
        if (dictionary.ContainsKey(key) == false) {
            dictionary.Add(key, new TDictionary());
        }

        return dictionary;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) {
            dictionary[key] = value;
        } else {
            dictionary.Add(key, value);
        }
    }

    public static TValue ReturnAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
        dictionary.Add(key, value);
        return value;
    }

    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> pair) => dictionary.AutoAdd(pair.Key, pair.Value);

    public static List<TValue> AutoAdd<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key) => dictionary.AutoAdd<List<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Add(value);

    public static HashSet<TValue> AutoAdd<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dictionary, TKey key) => dictionary.AutoAdd<HashSet<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Add(value);

    public static Queue<TValue> AutoAdd<TKey, TValue>(this IDictionary<TKey, Queue<TValue>> dictionary, TKey key) => dictionary.AutoAdd<Queue<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, Queue<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Enqueue(value);
    
    public static ConcurrentQueue<TValue> AutoAdd<TKey, TValue>(this IDictionary<TKey, ConcurrentQueue<TValue>> dictionary, TKey key) => dictionary.AutoAdd<ConcurrentQueue<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this IDictionary<TKey, ConcurrentQueue<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Enqueue(value);

    public static Dictionary<TIKey, TValue> AutoAdd<TKey, TIKey, TValue>(this IDictionary<TKey, Dictionary<TIKey, TValue>> dictionary, TKey outKey) => dictionary.AutoAdd<Dictionary<TIKey, TValue>, TKey, TIKey, TValue>(outKey)[outKey];
    public static void AutoAdd<TKey, TIKey, TValue>(this IDictionary<TKey, Dictionary<TIKey, TValue>> dictionary, TKey outKey, TIKey innerKey, TValue value) => dictionary.AutoAdd<Dictionary<TIKey, TValue>, TKey, TIKey, TValue>(outKey)[outKey].AutoAdd(innerKey, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ConcurrentDictionary<TKey, TCollection> AutoAdd<TCollection, TKey, TValue>(this ConcurrentDictionary<TKey, TCollection> dictionary, TKey key) where TCollection : IEnumerable<TValue>, new() => dictionary.AddOrUpdate(key, _ => new TCollection(), (_, collection) => collection).Pipe(_ => dictionary);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ConcurrentDictionary<TKey, TDictionary> AutoAdd<TDictionary, TKey, TIKey, TValue>(this ConcurrentDictionary<TKey, TDictionary> dictionary, TKey key) where TDictionary : IDictionary<TIKey, TValue>, new() => dictionary.AddOrUpdate(key, _ => new TDictionary(), (_, innerDic) => innerDic).Pipe(_ => dictionary);

    public static List<TValue> AutoAdd<TKey, TValue>(this ConcurrentDictionary<TKey, List<TValue>> dictionary, TKey key) => dictionary.AutoAdd<List<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this ConcurrentDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Add(value);
    
    public static Queue<TValue> AutoAdd<TKey, TValue>(this ConcurrentDictionary<TKey, Queue<TValue>> dictionary, TKey key) => dictionary.AutoAdd<Queue<TValue>, TKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TValue>(this ConcurrentDictionary<TKey, Queue<TValue>> dictionary, TKey key, TValue value) => dictionary.AutoAdd(key).Enqueue(value);

    public static ConcurrentDictionary<TIKey, TValue> AutoAdd<TKey, TIKey, TValue>(this ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>> dictionary, TKey key) => dictionary.AutoAdd<ConcurrentDictionary<TIKey, TValue>, TKey, TIKey, TValue>(key)[key];
    public static void AutoAdd<TKey, TIKey, TValue>(this ConcurrentDictionary<TKey, ConcurrentDictionary<TIKey, TValue>> dictionary, TKey key, TIKey innerKey, TValue value) => dictionary.AutoAdd(key).TryAdd(innerKey, value);

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new() {
        if (dictionary.TryGetValue(key, out var value) == false) {
            value = new TValue();
            dictionary.Add(key, value);
        }

        return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> creator) {
        if (dictionary.TryGetValue(key, out var value) == false) {
            value = creator.Invoke();
            dictionary.Add(key, value);
        }
        
        return value;
    }

    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> creator, Action<TKey, TValue> updater) {
        if (dictionary.TryGetValue(key, out var value)) {
            updater.Invoke(key, value);
            return;
        }
        
        dictionary.Add(key, creator.Invoke());
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

    public static bool TryCastValue<TKey, TValue, TCast>(this IDictionary<TKey, TValue> dictionary, TKey key, out TCast outValue) where TCast : TValue {
        dictionary.ThrowIfNull();
        if (dictionary.TryGetValue(key, out var value) && value is TCast castValue) {
            outValue = castValue;
            return true;
        }
        
        outValue = default;
        return false;
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
}