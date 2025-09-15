using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PlasticGui.WorkspaceWindow.Merge;
using UnityEngine;

[RefactoringRequired(5, "전체적으로 모든 메소드를 점검하고 리팩토링 필요. 메소드 수가 많아지고 기간이 길어지면서 코드의 일관성이 줄어듬")]
public static partial class CollectionExtension {

    #region [Common]

    public static IEnumerator CloneEnumerator(this ICollection collection) => new ArrayList(collection).GetEnumerator();
    public static IEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, bool isAscending) => isAscending ? enumerable.OrderBy(keySelector) : enumerable.OrderByDescending(keySelector) as IEnumerable<TSource>;
    public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> selector) where TResult : class => enumerable.Select(selector).WhereNotNull();
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable) where T : class => enumerable.Where(source => source != null);

    public static IEnumerable<TResult> SelectWhere<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> selector, Func<TResult, bool> predicate) {
        selector.ThrowIfNull(nameof(selector));
        predicate.ThrowIfNull(nameof(predicate));

        foreach (var source in enumerable) {
            var result = selector.Invoke(source);
            if (predicate.Invoke(result)) {
                yield return result;
            }
        }
    }

    public static IEnumerable<TResult> WhereSelect<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, bool> predicate, Func<TSource, TResult> selector) {
        predicate.ThrowIfNull(nameof(predicate));
        selector.ThrowIfNull(nameof(selector));
        
        foreach (var source in enumerable) {
            if (predicate.Invoke(source)) {
                yield return selector.Invoke(source);
            }
        }
    }

    public static IEnumerable<TResult> WhereSelectMany<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, bool> predicate, Func<TSource, IEnumerable<TResult>> selector) {
        predicate.ThrowIfNull(nameof(predicate));
        selector.ThrowIfNull(nameof(selector));
        
        foreach (var source in enumerable) {
            if (predicate.Invoke(source)) {
                foreach (var result in selector.Invoke(source)) {
                    yield return result;
                }
            }
        }
    }

    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> enumerable) => enumerable.SelectMany(values => values);
    public static IEnumerable<TResult> DistinctWithSelect<TValue, TResult>(this IEnumerable<TValue> enumerable, Func<TValue, TResult> converter, IEqualityComparer<TValue> equalityComparer = null) => enumerable.Distinct(equalityComparer).Select(converter.Invoke);
    public static IEnumerable<TResult> SelectWithDistinct<TValue, TResult>(this IEnumerable<TValue> enumerable, Func<TValue, TResult> converter, IEqualityComparer<TResult> equalityComparer = null) => enumerable.Select(converter.Invoke).Distinct(equalityComparer);

    public static bool TryFirst<T>(this IEnumerable<T> enumerable, out T matchItem, Predicate<T> match = null) {
        if (match == null) {
            matchItem = enumerable.First();
            return matchItem != null;
        }

        foreach (var item in enumerable) {
            if (match.Invoke(item)) {
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

    public static IEnumerable<T> Select<T>(this ICollection collection, Func<object, T> selector) {
        collection.ThrowIfNull(nameof(collection));
        selector.ThrowIfNull(nameof(selector));
        foreach (var obj in collection) {
            yield return selector.Invoke(obj);
        }
    }
    
    public static IEnumerable<Task> AsyncForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
        action.ThrowIfNull();
        if (enumerable != null) {
            foreach (var item in enumerable) {
                action.Invoke(item);
                yield return null;
            }
        }
    }

    public static bool TryCast<T>(this IEnumerable enumerable, out IEnumerable<T> cast) {
        cast = enumerable?.Cast<T>();
        return cast != null;
    }

    public static Vector2 SumVector<T>(this IEnumerable<T> source, Func<T, Vector2> selector) => source.Select(selector).Aggregate((x, y) => x + y);
    public static Vector3 SumVector<T>(this IEnumerable<T> source, Func<T, Vector3> selector) => source.Select(selector).Aggregate((x, y) => x + y);

    public static bool All(this IEnumerable<bool> enumerable) => enumerable.All(result => result);

    [TestRequired]
    [RefactoringRequired("코드 최적화 필요")]
    public static bool LateAll<TSource>(this IEnumerable<TSource> enumerable, Func<TSource, bool> predicate) {
        var lateResult = true;
        foreach (var source in enumerable) {
            if (predicate.Invoke(source) == false) {
                lateResult = false;
            }
        }

        return lateResult;
    }

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

    public static List<T> ToList<T>(this IList list) => list.Cast<T>().ToList();

    public static List<T> ToList<T>(this IList list, Func<object, T> selector) {
        var returnList = new List<T>();
        foreach (var obj in list) {
            returnList.Add(selector.Invoke(obj));
        }

        return returnList;
    }
    
    public static List<T> ToList<T>(this ReadOnlySpan<T> span) {
        var list = new List<T>();
        foreach (var value in span) {
            list.Add(value);
        }

        return list;
    }

    public static List<TResult> ToList<TValue, TResult>(this IEnumerable<TValue> enumerable, Func<TValue, TResult> converter) => enumerable.Select(converter).ToList();

    public static SortedList<TResultKey, TResultValue> ToSortedList<TValue, TResultKey, TResultValue>(this IEnumerable<TValue> enumerable, Func<TValue, TResultKey> keySelector, Func<TValue, TResultValue> valueSelector) {
        var sortedList = new SortedList<TResultKey, TResultValue>();
        foreach (var value in enumerable) {
            sortedList.Add(keySelector.Invoke(value), valueSelector.Invoke(value));
        }

        return sortedList;
    }

    public static TResult[] ToArray<TValue, TResult>(this ReadOnlySpan<TValue> span, Func<TValue, TResult> converter) {
        var array = new TResult[span.Length];
        for (var i = 0; i < array.Length; i++) {
            array[i] = converter.Invoke(span[i]);
        }

        return array;
    }
    
    public static TResult[] ToArray<TValue, TResult>(this IEnumerable<TValue> collection, Func<TValue, TResult> converter) => collection.Select(converter).ToArray();

    public static ImmutableArray<TResult> ToImmutableArray<TValue, TResult>(this ReadOnlySpan<TValue> span, Func<TValue, TResult> converter) => span.ToArray(converter).ToImmutableArray();

    public static ImmutableDictionary<TKey, TEnumerable> ToImmutableDictionary<TKey, TValue, TEnumerable>(this IEnumerable<IGrouping<TKey, TValue>> enumerable, Func<IGrouping<TKey, TValue>, TEnumerable> creator) where TEnumerable : IEnumerable<TValue> => enumerable.ToImmutableDictionary(grouping => grouping.Key, creator.Invoke);

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionaryWithDistinct<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionaryWithDistinct(keySelector, valueSelector).ToImmutableDictionary();

    public static Dictionary<TKey, TValue> ToDictionaryWithDistinct<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionary<Dictionary<TKey, TValue>, TSource, TKey, TValue>(keySelector, valueSelector);

    public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector) => enumerable.ToDictionary<Dictionary<TKey, TSource>, TSource, TKey, TSource>(keySelector, source => source);
    public static Dictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionary<Dictionary<TKey, TValue>, TSource, TKey, TValue>(keySelector, valueSelector);
    
    private static TDictionary ToDictionary<TDictionary, TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TDictionary : IDictionary<TKey, TValue>, new() {
        var dictionary = new TDictionary();
        foreach (var source in enumerable) {
            dictionary.AutoAdd(keySelector.Invoke(source), valueSelector.Invoke(source));
        }

        return dictionary;
    }
    
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) => enumerable.ToDictionary<ConcurrentDictionary<TKey, TValue>, TSource, TKey, TValue>(keySelector, valueSelector);

    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector) => keySelector.ThrowIfNull(nameof(keySelector)).Pipe(_ => enumerable.GroupBy(keySelector, source => source));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> func) => func.Invoke(value);
    
    #endregion

    #region [Queue]

    public static void Clear<T>(this Queue<T> queue, [NotNull]Func<T, bool> match) {
        if (match == null) {
            return;
        }
        
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
    public static ImmutableHashSet<TResult> ToImmutableHashSetForLinq<TValue, TResult>(this IEnumerable<TValue> enumerable, Func<TValue, TResult> converter) => enumerable.DistinctWithSelect(converter).ToImmutableHashSet();

    #endregion
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfNull<T>(T func, string name) where T : Delegate => func.ThrowIfNull(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfInvalidCast<T>(IEnumerable enumerable, string name) where T : IEnumerable {
        if (enumerable is not T) {
            throw new InvalidCastException<T>(name);
        }
    }
}