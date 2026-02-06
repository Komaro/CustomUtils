using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static partial class CollectionExtension {

    public static List<T> ToList<T>(this Array array) {
        var list = new List<T>();
        foreach (var obj in array) {
            if (obj is T value) {
                list.Add(value);
            }
        }

        return list;
    }

    public static bool TryToArray<T>(this Array array, out T[] result) => (result = array.ToArray<T>()) != Array.Empty<T>();

    public static T[] ToArray<T>(this Array array) {
        try {
            return array.Cast<T>().ToArray();
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<T>();
        }
    }

    public static bool TryToArray<TSource, TResult>(this TSource[] array, out TResult[] result, Func<TSource, TResult> converter) => (result = array.ToArray(converter)) != Array.Empty<TResult>();

    public static TResult[] ToArray<TSource, TResult>(this TSource[] array, Func<TSource, TResult> converter) {
        try {
            var returnArray = new TResult[array.Length];
            for (var index = 0; index < returnArray.Length; index++) {
                returnArray[index] = converter.Invoke(array[index]);
            }

            return returnArray;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<TResult>();
        }
    }

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

    public static bool TryGetRandom<T>(this T[] array, out T value) => (value = array.GetRandom()) != null;

    public static T GetRandom<T>(this T[] array) {
        try {
            if (array is { Length: > 0 }) {
                return array[UnityEngine.Random.Range(0, array.Length)];
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return default;
    }

    public static IEnumerable<T> GetRandoms<T>(this T[] array, int count = 10) {
        if (array is not { Length: > 0 }) {
            yield break;
        }

        while (--count > 0) {
            yield return array[RandomUtil.GetRandom(0, array.Length)];
        }
    }

    public static bool IsNotEmpty<TValue>(this TValue[] array) {
        try {
            array.ThrowIfNull(nameof(array));
            return array.Length > 0;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }
    
    public static bool IsEmpty<TValue>(this TValue[] array) {
        try {
            array.ThrowIfNull(nameof(array));
            return array.Length <= 0;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIndex<T>(this T[] array, int index) => array != null && index > -1 && array.Length > index;
}