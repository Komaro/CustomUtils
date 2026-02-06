using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class CollectionUtil {

    private static readonly Dictionary<Type, Dictionary<Type, IEnumerable>> _emptyDic = new();

    public static TCollection Empty<TCollection, TValue>() where TCollection : class, ICollection<TValue>, IEnumerable, new() => EmptyEnumerable<TCollection, TValue>() as TCollection;

    private static IEnumerable EmptyEnumerable<TCollection, TValue>() where TCollection : class, ICollection<TValue>, IEnumerable, new() {
        if (_emptyDic.TryGetValue(typeof(TCollection).GetGenericTypeDefinition(), out var dic) == false) {
            _emptyDic.Add(typeof(TCollection).GetGenericTypeDefinition(), dic = new Dictionary<Type, IEnumerable>());
        }

        if (dic.TryGetValue(typeof(TValue), out var enumerable) == false) {
            dic.Add(typeof(TValue), enumerable = new TCollection());
        }
        
        return enumerable;
    }

    public static class Collection {
        
        public static Collection<T> Empty<T>() => Empty<Collection<T>, T>();
    }

    public static class List {

        public static List<T> Empty<T>() => Empty<List<T>, T>();
    }

    public static class HashSet {

        public static HashSet<T> Empty<T>() => Empty<HashSet<T>, T>();
    }
}