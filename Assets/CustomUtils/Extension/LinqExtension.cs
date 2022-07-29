using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class LinqExtension 
{
    #region Generic
    
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) 
    {
		foreach (var item in enumerable) 
			action?.Invoke(item);
        
		return enumerable;
	}

    #endregion
	
	#region Dictionary
	
	public static void AutoAdd<T, V>(this IDictionary<T, V> dictionary, T key, V value) 
    {
        if (dictionary.ContainsKey(key)) 
			dictionary[key] = value;
        else 
			dictionary.Add(key, value);
	}

	public static void AutoAdd<T, V>(this Dictionary<T, List<V>> dictionary, T key, V value) 
    {
		if (dictionary.ContainsKey(key)) 
			dictionary[key].Add(value);
		else 
			dictionary.Add(key, new List<V>{ value });
    }
    
	public static void AutoAdd<T, V, K>(this Dictionary<T, Dictionary<V, K>> dictionary, T outKey, V innerKey, K value) 
    {
		if (dictionary.ContainsKey(outKey)) 
			dictionary[outKey].AutoAdd(innerKey, value);
		else 
			dictionary.Add(outKey, new Dictionary<V, K> {{ innerKey, value }});
    }

	public static void AutoCountingAdd<T, V>(this Dictionary<T, Dictionary<int, V>> dictionary, T key, V value) 
    {
		if (dictionary.ContainsKey(key)) 
			dictionary[key].Add(dictionary[key].Count + 1, value);
		else 
			dictionary.Add(key, new Dictionary<int, V> {{ 1, value }});
    }

	public static void AutoIncreaseAdd<T>(this IDictionary<T, int> dictionary, T key) 
    {
		if (dictionary.ContainsKey(key)) 
			dictionary[key] += 1;
		else 
			dictionary.Add(key, 1);
    }

	public static void AutoAccumulateAdd<T>(this IDictionary<T, int> dictionary, T key, int value) 
    {
		if (dictionary.ContainsKey(key)) 
			dictionary[key] += value;
		else 
			dictionary.Add(key, value);
    }

    public static void AutoAccumulateAdd<T>(this IDictionary<T, long> dictionary, T key, long value) 
    {
        if (dictionary.ContainsKey(key)) 
            dictionary[key] += value;
        else 
            dictionary.Add(key, value);
    }

	public static void AutoAccumulateAdd<T, V>(this IDictionary<T, IDictionary<V, int>> dictionary, T outKey, V innerKey, int value)
    {
		if (dictionary.ContainsKey(outKey)) 
			dictionary[outKey].AutoAccumulateAdd(innerKey, value);
		else
			dictionary.Add(outKey, new Dictionary<V, int> {{ innerKey, value }});
    }

    
    public static V FindValue<T, V>(this IDictionary<T, V> dictionary, Func<T, V, bool> match)
    {
        var pair = dictionary.FirstOrDefault(x => match?.Invoke(x.Key, x.Value) ?? false);
        return pair.Equals(default(KeyValuePair<T, V>)) ? default : pair.Value;
    }

    public static bool TryFindValue<T, V>(this IDictionary<T, V> dictionary, out V value, Func<T, V, bool> match) 
    {
        value = dictionary.FindValue(match);
        return value != null;
    }
    
    public static List<V> FindAllValue<T, V>(this IDictionary<T, V> dictionary, Func<T, V, bool> match) => dictionary.Where(x => match?.Invoke(x.Key, x.Value) ?? false).Select(x => x.Value).ToList();

    public static int FindKeyIndex<T, V>(this IDictionary<T, V> dictionary, T key) 
    {
        int index = 0;
        foreach (var keyItem in dictionary.Keys) 
        {
            if (keyItem.Equals(key)) 
                return index;
            
            index++;
        }

        return -1;
    }
    
    public static bool TryFindKeyIndex<T, V>(this IDictionary<T, V> dictionary, T key, out int index) 
    {
        index = FindKeyIndex(dictionary, key);
        return index >= 0;
    }

    public static string ToStringCollection<T, V>(this IDictionary<T, V> dictionary) => string.Join(' ', dictionary.Select(x => $"({x.Key} , {x.Value})"));

    #endregion

	#region List

    public static void AutoAdd<T>(this List<T> list, Predicate<T> match, T item) 
    {
        if (list.TryFindIndex(match, out var index)) 
            list[index] = item;
        else
            list.Add(item);
    }

    public static void Sync<T, V>(this List<T> baseList, List<V> targetList, Func<T> createFunc) => ISync(baseList, targetList, createFunc);

    public static void ISync<T>(this List<T> baseList, IList targetList, Func<T> createFunc) 
    {
        
        if (baseList.Count < targetList.Count) 
        {
            var syncCount = targetList.Count - baseList.Count;
            for (var i = 0; i < syncCount; i++) 
                baseList.Add(createFunc.Invoke());
        }
    }

    public static void ISync<T>(this IList baseList, List<T> targetList, Func<T> createFunc) 
    {
        if (baseList.Count < targetList.Count) 
        {
            var syncCount = targetList.Count - baseList.Count;
            for (int i = 0; i < syncCount; i++) 
                baseList.Add(createFunc.Invoke());
        }
    }
    
    public static void Sync<T, V>(this List<T> baseList, List<V> targetList, Func<V> createFunc, Action<V> removeAction) => ISync(baseList, targetList, createFunc, removeAction);
    
    public static void ISync<T, V>(this List<T> baseList, IList targetList, Func<V> createFunc, Action<V> removeAction)
    {
        var count = baseList.Count - targetList.Count;
        
        while (count++ < 0) 
        {
            if (targetList[0] is V item) 
                removeAction?.Invoke(item);    
            
            targetList.RemoveAt(0);
        }

        while (count-- > 0) 
        {
            targetList.Add(createFunc.Invoke());
        }
    }

    public static void SyncForEach<T, V>(this List<T> baseList, List<V> targetList, Action<T, V> dataAction, Action<V> clearAction = null) 
    {
        for (var i = 0; i < targetList.Count; i++) 
        {
			if (i >= baseList.Count) 
            {
                for (var j = i; j < targetList.Count; j++) 
                    clearAction?.Invoke(targetList[j]);
                
                return;
			}

            dataAction?.Invoke(baseList[i], targetList[i]);
        }
	}

    public static void SyncForEach<T, V>(this List<T> baseList, List<V> targetList, Func<int, T, V, bool> checkAction, Action<T, V> dataAction, Action<V> clearAction = null) 
    {
        if (baseList.Count <= 0) 
            return;
        
        var targetIndex = 0;
        while (targetIndex < targetList.Count) 
        {
            var baseIndex = 0;
            do 
            {
                if (checkAction?.Invoke(targetIndex, baseList[baseIndex], targetList[targetIndex]) ?? false) 
                {
                    dataAction?.Invoke(baseList[baseIndex], targetList[targetIndex]);
                    break;
                }
                baseIndex++;
            } 
            while (baseIndex < baseList.Count);

            if (baseIndex >= baseList.Count) {
                clearAction?.Invoke(targetList[targetIndex]);
            }
            
            targetIndex++;
        }
    }

    public static void ISyncForEach<V>(this IList baseList, List<V> targetList, Action<object, V> dataAction, Action<V> clearAction = null)
    {
        for (var i = 0; i < targetList.Count; i++) 
        {
            if (i >= baseList.Count) 
            {
                for (var j = i; j < targetList.Count; j++) 
                    clearAction?.Invoke(targetList[j]);
                
                return;
            }
			
            dataAction?.Invoke(baseList[i], targetList[i]);
        }
    }

    public static bool TryFind<T>(this List<T> baseList, int index, out T value) 
    {
        if (index + 1 > baseList.Count || index < 0) 
        {
            value = default;
            return false;
        }

        value = baseList[index];
        return true;
    }

    public static bool TryFind<T>(this List<T> list, Predicate<T> match, out T value) 
    {
        value = list.Find(match);
        return value != null;
    }

    public static bool TryFindIndex<T>(this List<T> list, Predicate<T> match, out int index) 
    {
        index = list.FindIndex(match);
        return index != -1;
    }
    
    public static List<V> ConvertTo<T, V>(this IEnumerable<T> collection, Func<T, V> converter) where T : new()
    {
        var convertList = new List<V>();
        foreach (var item in collection) 
        {
            var convertItem = converter.Invoke(item);
            if (convertItem == null)
                continue;
            
            convertList.Add(convertItem);
        }

        return convertList;
    }

    public static string ToStringCollection<T, V>(this IList list) => string.Join(' ', list);
	
	// Fisher–Yates shuffle 기반
	private static readonly Random _randomGenerator = new Random();
	
	public static List<T> Shuffle<T>(this List<T> list, Predicate<T> match) => Shuffle(list).FindAll(match);
	
	public static List<T> Shuffle<T>(this List<T> list) 
    {
		try 
        {
			var n = list.Count;
			while (n > 1)
            {
				var k = _randomGenerator.Next(--n + 1);
				(list[k], list[n]) = (list[n], list[k]);
			}
		} 
        catch (Exception ex) 
        {
            Logger.TraceError(ex.Message);
			return new List<T>();
		}

		return list;
	}
		
	#endregion
}
