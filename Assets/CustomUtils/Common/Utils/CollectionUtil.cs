
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

public static class CollectionUtil {

    internal static readonly Dictionary<Type, Dictionary<Type, IEnumerable>> emptyDic = new();

    private static readonly Dictionary<Type, ICollection> _emptyDic = new();

    internal static IEnumerable EmptyEnumerable<TCollection, TValue>() where TCollection : IEnumerable, new() {
        if (emptyDic.TryGetValue(typeof(TCollection).GetGenericTypeDefinition(), out var dic) == false) {
            emptyDic.Add(typeof(TCollection), dic = new Dictionary<Type, IEnumerable>());
        }

        if (dic.TryGetValue(typeof(TValue), out var enumerable) == false) {
            dic.Add(typeof(TValue), enumerable = new TCollection());
        }

        return enumerable;
    }

    public class Collection {
        
        public static Collection<T> Empty<T>() => EmptyEnumerable<Collection<T>, T>() as Collection<T>;
    }

    public static class List {

        public static List<T> Empty<T>() => EmptyEnumerable<List<T>, T>() as List<T>;
    }

    public static class HashSet {

        public static HashSet<T> Empty<T>() => EmptyEnumerable<HashSet<T>, T>() as HashSet<T>;
    }
}