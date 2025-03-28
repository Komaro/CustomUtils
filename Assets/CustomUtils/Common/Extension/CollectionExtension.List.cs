using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static partial class CollectionExtension {

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
        if (sourceList.Count <= 0) {
            return;
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIndex<T>(this List<T> list, int index) {
        if (list == null) {
            return false;
        }

        return index > -1 && list.Count > index;
    }

    // public static List<TResult> ConvertTo<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> converter) {
    //     var convertList = new List<TResult>();
    //     foreach (var item in enumerable) {
    //         var convertItem = converter.Invoke(item);
    //         if (convertItem == null) {
    //             continue;
    //         }
    //
    //         convertList.Add(convertItem);
    //     }
    //
    //     return convertList;
    // }

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

    #endregion
}