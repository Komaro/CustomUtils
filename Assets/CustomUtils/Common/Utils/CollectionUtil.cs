
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class CollectionUtil {

    private static ConcurrentDictionary<Type, ICollection> _emptyDic = new();

    public static Collection<T> Empty<T>() {
        if (_emptyDic.TryGetValue(typeof(T), out var collection) == false) {
            _emptyDic.TryAdd(typeof(T), new Collection<T>());
        }
        
        return collection as Collection<T>;
    }

    public static class List {

        private static readonly ConcurrentDictionary<Type, IList> _emptyDic = new();

        public static List<T> Empty<T>() {
            if (_emptyDic.TryGetValue(typeof(T), out var list) == false) {
                _emptyDic.TryAdd(typeof(T), new List<T>());
            }

            return list as List<T>;
        }
    }
}