using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public abstract class GlobalEnum {

    // TODO. 캐싱 종류가 많고 Key, Value 타입들이 구체적이지 않아 이해가 어려움
    // TODO. 규격화 된 캐싱 구조가 필요
    protected static readonly Dictionary<Type, ImmutableSortedDictionary<Type, ImmutableHashSet<Enum>>> enumSetDic = new();
    // protected static readonly Dictionary<Type, ImmutableHashSet<Type>> enumTypeSetDic = new();
    protected static readonly Dictionary<Type, ImmutableDictionary<int, Enum>> intToEnumDic = new();
    protected static readonly Dictionary<Type, ImmutableDictionary<Enum, int>> enumToIntDic = new();
}

[Serializable]
[DebuggerDisplay("Value = {Value} index = {_index}")]
public sealed class GlobalEnum<TAttribute> : GlobalEnum, IEnumerable<Enum> where TAttribute : PriorityAttribute {

    private readonly HashSet<Type> _enumTypeSet = new();
    
    private int _index;

    public int Index {
        get => _index;
        set {
            if (intToEnumDic[typeof(TAttribute)].ContainsKey(value)) {
                _index = value;
            } else {
                _index = 0;
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    public Enum Value {
        get => intToEnumDic[typeof(TAttribute)][_index];
        set {
            if (enumToIntDic[typeof(TAttribute)].TryGetValue(value, out _index) == false) {
                Logger.TraceError($"The {value} does not have a type of {typeof(TAttribute).Name}");
            }
        }
    }

    public int Count => enumToIntDic[typeof(TAttribute)].Count;
    public IEnumerable<Enum> Values => this;
    
    public Enum this[int index] => intToEnumDic[typeof(TAttribute)].TryGetValue(index, out var enumValue) ? enumValue : null;
    public ImmutableHashSet<Enum> this[Type type] => enumSetDic[typeof(TAttribute)].TryGetValue(type, out var enumSet) ? enumSet : ImmutableHashSet<Enum>.Empty;
    
    public static explicit operator Enum(GlobalEnum<TAttribute> globalEnum) => globalEnum.Value;
    public static explicit operator GlobalEnum<TAttribute>(Enum enumValue) => new(enumValue);
    
    static GlobalEnum() {
        if (ReflectionProvider.TryGetAttributeEnumInfos<TAttribute>(out var enumerable)) {
            if (enumSetDic.ContainsKey(typeof(TAttribute)) == false) {
                var index = 0;
                enumSetDic.Add(typeof(TAttribute), enumerable.ToImmutableSortedDictionary(info => info.enumType, info => info.enumType.GetEnumValues().OfType<Enum>().ToImmutableHashSet(), new GlobalEnumPriorityComparer()));
                // enumTypeSetDic.TryAdd(typeof(TAttribute), enumSetDic[typeof(TAttribute)].Keys.ToImmutableHashSet());
                intToEnumDic.TryAdd(typeof(TAttribute), enumSetDic[typeof(TAttribute)].Values.SelectMany(enumSet => enumSet).ToImmutableDictionary(_ => index++, enumValue => enumValue));
                enumToIntDic.TryAdd(typeof(TAttribute), intToEnumDic[typeof(TAttribute)].ToImmutableDictionary(pair => pair.Value, pair => pair.Key));
            }
        } else {
            Logger.TraceLog($"Cannot find an enum type with the {nameof(TAttribute)}({typeof(TAttribute).GetCleanFullName()})", Color.Yellow);
        }
    }
    
    public GlobalEnum() {
        if (intToEnumDic[typeof(TAttribute)].TryGetValue(0, out var enumValue)) {
            Value = enumValue;
        }
    }
    
    public GlobalEnum(Enum enumValue) => Value = enumValue;

    public Enum Get() => Value;
    public TEnum Get<TEnum>() where TEnum : struct, Enum => Value.Convert<TEnum>();
    
    // TODO. 캐싱 규격화 이후 제거
    // public TEnum Get_Unsafe<TEnum>() where TEnum : struct, Enum => (TEnum)Value;
    // public TEnum Get_Before<TEnum>() where TEnum : struct, Enum => Value.Convert<TEnum>();
    // public TEnum Get_After<TEnum>() where TEnum : struct, Enum => enumSetDic[typeof(TAttribute)].Keys.Contains(typeof(TEnum)) ? (TEnum)Value : default;
    // public TEnum Get_CacheAfter<TEnum>() where TEnum : struct, Enum => enumTypeSetDic[typeof(TAttribute)].Contains(typeof(TEnum)) ? (TEnum)Value : default;

    public void Set(Enum enumValue) => Value = enumValue;
    public void Set<TEnum>(TEnum enumValue) where TEnum : struct, Enum => Value = enumValue;

    public bool Contains(Enum enumValue) => enumToIntDic[typeof(TAttribute)].ContainsKey(enumValue);
    public bool Contains<TEnum>(TEnum enumValue) where TEnum : struct, Enum => enumSetDic[typeof(TAttribute)].TryGetValue(typeof(TEnum), out var enumSet) && enumSet.Contains(enumValue);
    
    public IEnumerator<Enum> GetEnumerator() => intToEnumDic[typeof(TAttribute)].Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class GlobalEnumPriorityComparer : IComparer<Type> {
        
        public int Compare(Type xType, Type yType) {
            if (xType == null || yType == null) {
                return 0;
            }

            return xType.TryGetCustomAttribute<PriorityAttribute>(out var xAttribute) && yType.TryGetCustomAttribute<PriorityAttribute>(out var yAttribute) ? xAttribute.priority.CompareTo(yAttribute.priority) : 0;
        }
    }
}