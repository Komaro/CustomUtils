using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

[Serializable]
public sealed class GlobalEnum<TAttribute> : IEnumerable<Enum> where TAttribute : PriorityAttribute {

    private static readonly ImmutableDictionary<Type, ImmutableHashSet<Enum>> _enumSetDic;
    private static readonly ImmutableDictionary<int, Enum> _intToEnumDic;
    private static readonly ImmutableDictionary<Enum, int> _enumToIntDic;

    private int _index;

    public Enum Value {
        get => _intToEnumDic[_index];
        set {
            if (_enumToIntDic.TryGetValue(value, out _index) == false) {
                Logger.TraceError($"The {value} does not have a type of {typeof(TAttribute).Name}");
            }
        }
    }

    public int Count { get; }
    public IEnumerable<Enum> Values => this;

    public Enum this[int index] => _intToEnumDic.TryGetValue(index, out var enumValue) ? enumValue : null;
    public ImmutableHashSet<Enum> this[Type type] => _enumSetDic.TryGetValue(type, out var enumSet) ? enumSet : ImmutableHashSet<Enum>.Empty;

    static GlobalEnum() {
        if (ReflectionProvider.TryGetAttributeEnumInfos<TAttribute>(out var enumerable)) {
            _enumSetDic = enumerable.OrderBy(info => info.attribute?.priority ?? 99999)
                .ToImmutableDictionary(info => info.enumType, info => info.enumType.GetEnumValues().OfType<Enum>().ToImmutableHashSet());

            var index = 0;
            _intToEnumDic = _enumSetDic.Values.SelectMany(enumSet => enumSet).ToImmutableDictionary(_ => index++, enumValue => enumValue);
            _enumToIntDic = _intToEnumDic.ToImmutableDictionary(pair => pair.Value, pair => pair.Key);
        }
    }

    public GlobalEnum() => Count = _enumToIntDic.Count;
    public TEnum Get<TEnum>() where TEnum : struct, Enum => Value.Convert<TEnum>();
    public void Set<TEnum>(TEnum enumValue) where TEnum : struct, Enum => Value = enumValue;
    public bool Contains<TEnum>(TEnum enumValue) where TEnum : struct, Enum => _enumSetDic.TryGetValue(typeof(TEnum), out var enumSet) && enumSet.Contains(enumValue);
    
    public IEnumerator<Enum> GetEnumerator() => _intToEnumDic.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}