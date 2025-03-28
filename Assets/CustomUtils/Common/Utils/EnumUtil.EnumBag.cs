using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public static partial class EnumUtil {

    private static class EnumBag {

        private static readonly ConcurrentDictionary<Type, Bag> _enumBagDic = new();

        public static Enum Get(Type type, string stringValue) {
            if (type.IsEnum == false) {
                Logger.TraceError($"{nameof(type)} is not {nameof(Enum)} type");
                return null;
            }

            return _enumBagDic.GetOrAdd(type, _ => new Bag(type)).Get(stringValue);
        }

        public static IEnumerable<Enum> GetValues(Type type, bool ignoreObsolete = false) {
            if (type.IsEnum == false) {
                Logger.TraceError($"{nameof(type)} is not {nameof(Enum)} type");
                return Enumerable.Empty<Enum>();
            }

            return _enumBagDic.GetOrAdd(type, _ => new Bag(type)).GetValues(ignoreObsolete);
        }

        public static ReadOnlySpan<Enum> AsSpan(Type type, bool ignoreObsolete = false) {
            if (type.IsEnum == false) {
                Logger.TraceError($"{nameof(type)} is not {nameof(Enum)} type");
                return ReadOnlySpan<Enum>.Empty;
            }

            return _enumBagDic.GetOrAdd(type, _ => new Bag(type)).AsSpan(ignoreObsolete);
        }

        public sealed class Bag {

            private ImmutableArray<Enum> _values = ImmutableArray<Enum>.Empty;
            private ImmutableArray<Enum> _ignoreObsoleteValues = ImmutableArray<Enum>.Empty;
            private readonly ImmutableDictionary<string, Enum> _stringToEnumDic = ImmutableDictionary<string, Enum>.Empty;
            
            public Bag(Type type) {
                if (type.IsEnum == false) {
                    Logger.TraceError($"{type.FullName} {nameof(type)} is not enum type");
                    return;
                }
                
                _values = Enum.GetValues(type).ToArray<Enum>().OrderBy(enumValue => enumValue).ToImmutableArray();
                _ignoreObsoleteValues = _values.Where(enumValue => type.TryGetFieldInfo(out var info, enumValue.ToString()) && info.IsDefined<ObsoleteAttribute>() == false)
                    .ToImmutableArray();
                _stringToEnumDic = _values.ToImmutableDictionary(value => string.Intern(value.ToString()), value => value);
            }
            
            public Enum Get(string stringValue) => _stringToEnumDic.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
            public IEnumerable<Enum> GetValues(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues : _values;
            public ReadOnlySpan<Enum> AsSpan(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues.AsSpan() : _values.AsSpan();
        }
    }

    private static class EnumBag<TEnum> where TEnum : struct, Enum {

        private static ImmutableArray<TEnum> _values = ImmutableArray<TEnum>.Empty;
        private static ImmutableArray<TEnum> _ignoreObsoleteValues = ImmutableArray<TEnum>.Empty;
        private static readonly ImmutableDictionary<string, TEnum> _stringToEnumDic = ImmutableDictionary<string, TEnum>.Empty;

        static EnumBag() {
            var valuesSpan = EnumBag.AsSpan(typeof(TEnum));
            if (valuesSpan.IsEmpty) {
                Logger.TraceError($"{nameof(valuesSpan)} is empty");
                return;
            }

            _values = valuesSpan.ToImmutableArray(type => (TEnum) type);

            _values = valuesSpan.ToArray().ToArray<TEnum>().ToImmutableArray();
            _ignoreObsoleteValues = _values.Where(enumValue => typeof(TEnum).TryGetFieldInfo(out var info, enumValue.ToString()) && info.IsDefined<ObsoleteAttribute>() == false).ToImmutableArray();
            _stringToEnumDic = _values.ToImmutableDictionary(value => string.Intern(value.ToString()), value => value);
        }

        public static TEnum Get(string stringValue) => _stringToEnumDic.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
        public static IEnumerable<TEnum> GetValues(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues : _values;
        public static ReadOnlySpan<TEnum> AsSpan(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues.AsSpan() : _values.AsSpan();
    }
}