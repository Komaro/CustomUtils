using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static partial class CollectionExtension {

    public static void ThrowIfInvalidKey<T>(this IList<T> list, int index) {
        if (list.IsValidIndex(index) == false) {
            throw new KeyNotFoundException($"{nameof(index)} is invalid || Range = 0 ~ {list.Count} || {nameof(index)} = {index}");
        }
    }

    [TestRequired]
    public static void Sync<T>(this IList<T> list, IList sourceList, Func<T> creator) {
        ThrowIfInvalidCast<IList<T>>(sourceList, nameof(sourceList));
        ThrowIfNull(creator, nameof(creator));
        
        var syncCount = sourceList.Count - list.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(out var result)) {
                    list.Add(result);
                }
            }
        }
    }
    
    // [Obsolete]
    // public static void ISync<T>(this List<T> workList, IList sourceList, Func<T> createFunc) {
    //     if (workList.Count < sourceList.Count) {
    //         var syncCount = sourceList.Count - workList.Count;
    //         for (var i = 0; i < syncCount; i++) {
    //             var item = createFunc.Invoke();
    //             if (item != null) {
    //                 workList.Add(item);
    //             }
    //         }
    //     }
    // }

    [TestRequired]
    public static void Sync<T>(this IList list, IList<T> sourceList, Func<T> creator) {
        ThrowIfInvalidCast<IList<T>>(list, nameof(list));
        ThrowIfNull(creator, nameof(creator));

        var syncCount = sourceList.Count - list.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(out var result)) {
                    list.Add(result);
                }
            }
        }
    }

    public static void Sync<T, V>(this IList<T> list, IList<V> sourceList, Func<V, T> creator) {
        list.ThrowIfNull(nameof(list));
        sourceList.ThrowIfNull(nameof(sourceList));
        creator.ThrowIfNull(nameof(creator));
        
        var syncCount = sourceList.Count - list.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(sourceList[i], out var result)) {
                    list.Add(result);
                }
            }
        }
    }
    
    [TestRequired]
    public static void Sync<TValue, TResult>(this IList list, IList<TValue> sourceList, Func<TValue, TResult> creator) {
        list.ThrowIfNull(nameof(list));
        sourceList.ThrowIfNull(nameof(sourceList));
        creator.ThrowIfNull(nameof(creator));
        
        var syncCount = sourceList.Count - list.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(sourceList[i], out var result)) {
                    list.Add(result);
                }
            }
        }
    }

    [TestRequired]
    public static void Sync<TValue, TResult>(this List<TResult> list, List<TValue> sourceList, Func<int, TValue, TResult> creator) {
        ThrowIfNull(creator, nameof(creator));
        var syncCount = sourceList.Count - list.Count;
        if (syncCount > 0) {
            for (var index = 0; index < syncCount; index++) {
                if (creator.TryInvoke(index ,sourceList[index], out var result)) {
                    list.Add(result);
                }
            }
        }
    }
    
    [TestRequired]
    public static void Sync<TList, TCollection>(this IList<TList> list, ICollection<TCollection> collection, Func<TList> creator) {
        var syncCount = collection.Count - list.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(out var result)) {
                    list.Add(result);
                }
            }
        }
    }
    
    [Obsolete("IDictionary의 범위가 한정적임")]
    public static void Sync<T>(this List<T> workList, IDictionary dictionary, Func<T> creator) {
        var syncCount = dictionary.Count - workList.Count;
        if (syncCount > 0) {
            for (var i = 0; i < syncCount; i++) {
                if (creator.TryInvoke(out var result)) {
                    workList.Add(result);
                }
            }
        }
        
        // if (syncCount > 0) {
        //     for (var i = 0; i < syncCount; i++) {
        //         var item = creator.Invoke();
        //         if (item != null) {
        //             workList.Add(item);
        //         }
        //     }
        // }
    }

    // [Obsolete]
    // public static void ISync<T>(this IList targetList, List<T> sourceList, Func<T> createFunc) {
    //     if (targetList.Count < sourceList.Count) {
    //         var syncCount = sourceList.Count - targetList.Count;
    //         for (var i = 0; i < syncCount; i++) {
    //             var item = createFunc.Invoke();
    //             if (item != null) {
    //                 targetList.Add(item);
    //             }
    //         }
    //     }
    // }
    
    [TestRequired]
    public static void Sync<T>(this IList<T> list, int maxCount, Func<T> creator) {
        ThrowIfNull(creator, nameof(creator));

        while (list.Count < maxCount) {
            list.Add(creator.Invoke());
        }

        while (list.Count > maxCount) {
            list.RemoveLast();
        }
    }

    [Obsolete]
    public static void Sync<T>(this List<T> list, int maxCount, Func<T> creator) {
        if (creator == null) {
            return;
        }

        while (list.Count < maxCount) {
            list.Add(creator.Invoke());
        }

        if (list.Count > maxCount) {
            list.RemoveRange(maxCount, list.Count - maxCount);
        }
    }

    [TestRequired]
    public static void Sync<TValue, TSource>(this List<TValue> list, List<TSource> sourceList, Func<TValue> creator, Action<TValue> cleaner) {
        ThrowIfNull(creator, nameof(creator));
        ThrowIfNull(cleaner, nameof(cleaner));

        var syncCount = list.Count - sourceList.Count;
        while (syncCount++ > 0) {
            cleaner.Invoke(list[0]);
            list.RemoveAt(0);
        }

        while (syncCount-- > 0) {
            if (creator.TryInvoke(out var result)) {
                list.Add(result);
            }
        }
    }
    
    [Obsolete("포멧 정리의 일환으로 위 메소드 형태로 전환")]
    public static void Sync<TSource, TWork>(this List<TSource> sourceList, List<TWork> workList, Func<TWork> creator, Action<TWork> cleaner) {
        ThrowIfNull(creator, nameof(creator));
        ThrowIfNull(cleaner, nameof(cleaner));
        
        var syncCount = sourceList.Count - workList.Count;
        while (syncCount++ < 0) {
            cleaner.Invoke(workList[0]);
            workList.RemoveAt(0);
        }

        while (syncCount-- > 0) {
            workList.Add(creator.Invoke());
        }
    }

    [TestRequired]
    public static void IndexForEach(this IList list, Action<object, int> action) {
        ThrowIfNull(action, nameof(action));
        for (var index = 0; index < list.Count; index++) {
            action.Invoke(list[index], index);
        }
    }

    [TestRequired]
    public static void IndexForEach<T>(this IList<T> list, Action<T, int> action) {
        ThrowIfNull(action, nameof(action));
        for (var index = 0; index < list.Count; index++) {
            action.Invoke(list[index], index);
        }
    }

    [Obsolete("기능이 불분명하며 성능상의 하자로 인해 제거 예정")]
    public static void ISyncForEach<TSource, TWork>(this IList<TSource> sourceList, IList<TWork> workList, Func<int, TSource, TWork, bool> checker, Action<TSource, TWork> setter, Action<TWork> cleaner = null) {
        if (sourceList.Count <= 0) {
            return;
        }

        var workIndex = 0;
        while (workIndex < workList.Count) {
            var index = 0;
            do {
                if (checker.Invoke(workIndex, sourceList[index], workList[workIndex])) {
                    setter.Invoke(sourceList[index], workList[workIndex]);
                    break;
                }

                index++;
            } while (index < sourceList.Count);

            if (index >= sourceList.Count) {
                cleaner?.Invoke(workList[workIndex]);
            }

            workIndex++;
        }
    }

    [TestRequired]
    public static void SyncForEach<T>(this IList<T> list, IList pairList, Action<T, object> action, Action<T> cleaner = null) {
        ThrowIfNull(action, nameof(action));
        var index = 0;
        if (list.Count - pairList.Count > 0) {
            for (; index < pairList.Count; index++) {
                action.Invoke(list[index], pairList[index]);
            }
        }

        if (cleaner != null && list.IsValidIndex(index)) {
            for (; index < list.Count; index++) {
                cleaner.Invoke(list[index]);
            }
        }
    }

    public static void SyncForEach<T>(this IList list, IList<T> pairList, Action<object, T> action, Action<object> cleaner = null) {
        ThrowIfNull(action, nameof(action));
        var index = 0;
        if (list.Count - pairList.Count > 0) {
            for (; index < pairList.Count; index++) {
                action.Invoke(list[index], pairList[index]);
            }
        }

        if (cleaner != null && list.IsValidIndex(index)) {
            for (; index < list.Count; index++) {
                cleaner.Invoke(list[index]);
            }
        }
    }
    
    [Obsolete("SyncForEach<T>로 전환")]
    public static void ISyncForEach<T>(this IList list, IList<T> pairList, Action<object, T> dataAction, Action<T> clearAction = null) {
        for (var i = 0; i < pairList.Count; i++) {
            if (i >= list.Count) {
                for (var j = i; j < pairList.Count; j++)
                    clearAction?.Invoke(pairList[j]);

                return;
            }

            dataAction?.Invoke(list[i], pairList[i]);
        }
    }

    [Obsolete("위 메소드로 전환 예정")]
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

    public static void LimitedAdd<T>(this List<T> list, T value, int maxCount) {
        if (list.Count >= maxCount) {
            list.RemoveAt(0);
        }

        list.Add(value);
    }

    public static void AutoAdd<T>(this List<T> list, Predicate<T> match, T value) {
        if (list.TryFindIndex(out var index, match)) {
            list[index] = value;
        } else {
            list.Add(value);
        }
    }

    public static bool RemoveAt<T>(this List<T> list, int index, out T value) {
        if (list.IsValidIndex(index)) {
            value = list[index];
            list.RemoveAt(index);
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryFirst<T>(this List<T> list, int index, out T value) {
        if (list.IsValidIndex(index)) {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryFind<T>(this List<T> list, int index, out T value) {
        if (list.IsValidIndex(index)) {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }
    
    public static bool TryFind<T>(this List<T> list, Predicate<T> predicate, out T value) {
        predicate.ThrowIfNull(nameof(predicate));
        value = list.Find(predicate);
        return value != null;
    }
    
    public static bool TryFirst<T>(this List<T> list, out T value, Predicate<T> match) => (value = list.Find(match)) != null;
    public static bool TryFindIndex<T>(this List<T> list, out int index, Predicate<T> match) => (index = list.FindIndex(match)) >= 0;

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

    public static List<T> SortList<T>(this List<T> list, Comparison<T> comparison) {
        list?.Sort(comparison);
        return list;
    }
    
    private static readonly Random _randomGenerator = new Random();
    
    public static List<T> Shuffle<T>(this IList<T> list, Predicate<T> match) => list.Shuffle().FindAll(match);

    public static List<T> Shuffle<T>(this IList<T> list) {
        try {
            var n = list.Count;
            while (n > 1) {
                var k = _randomGenerator.Next(--n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list as List<T>;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return CollectionUtil.List.Empty<T>();
        }
    }
    
    [TestRequired]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIndex(this IList list, int index) => list != null && index > -1 && list.Count > index;

    [TestRequired]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIndex<T>(this IList<T> list, int index) => list != null && index > -1 && list.Count > index;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIndex<T>(this List<T> list, int index) => list != null && index > -1 && list.Count > index;
}