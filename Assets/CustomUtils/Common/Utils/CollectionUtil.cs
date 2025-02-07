
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

public static class CollectionUtil {

    private static ConcurrentDictionary<Type, ICollection> _emptyDic = new();

    public static Collection<T> Empty<T>() {
        if (_emptyDic.TryGetValue(typeof(T), out var collection) == false) {
            _emptyDic.TryAdd(typeof(T), new Collection<T>());
        }
        
        return collection as Collection<T>;
    }
}
