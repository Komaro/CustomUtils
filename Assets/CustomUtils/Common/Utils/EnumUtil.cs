using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

internal static class EnumBag {

    private static readonly ConcurrentDictionary<Type, Bag> _enumBagDic = new();

    public static Enum Get(Type type, string stringValue) {
        if (type.IsEnum == false) {
            Logger.TraceError($"{nameof(type)} is not {nameof(Enum)} type");
            return null;
        }

        return _enumBagDic.GetOrAdd(type, _ => new Bag(type)).Get(stringValue);
    }

    public static ReadOnlySpan<Enum> GetValues(Type type, bool ignoreObsolete = false) {
        if (type.IsEnum == false) {
            Logger.TraceError($"{nameof(type)} is not {nameof(Enum)} type");
            return ReadOnlySpan<Enum>.Empty;
        }

        return _enumBagDic.GetOrAdd(type, _ => new Bag(type)).GetValues(ignoreObsolete);
    }

    private sealed class Bag {

        private ImmutableArray<Enum> _values = ImmutableArray<Enum>.Empty;
        private ImmutableArray<Enum> _ignoreObsoleteValues = ImmutableArray<Enum>.Empty;
        private readonly ImmutableDictionary<string, Enum> _stringToEnumDic = ImmutableDictionary<string, Enum>.Empty;

        public Bag(Type type) {
            if (type.IsEnum == false || type.IsArray) {
                Logger.TraceError($"{type.FullName} {nameof(type)} is not enum type");
                return;
            }

            _values = Enum.GetValues(type).ToArray<Enum>().OrderBy(enumValue => enumValue).ToImmutableArray();
            _ignoreObsoleteValues = _values.Where(enumValue => type.TryGetFieldInfo(out var info, enumValue.ToString()) && info.IsDefined<ObsoleteAttribute>() == false).ToImmutableArray();
            _stringToEnumDic = _values.ToImmutableDictionary(value => string.Intern(value.ToString()), value => value);
        }

        public Enum Get(string stringValue) => _stringToEnumDic.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
        public ReadOnlySpan<Enum> GetValues(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues.AsSpan() : _values.AsSpan();
    }
}

internal static class EnumBag<TEnum> where TEnum : struct, Enum {

    private static ImmutableArray<TEnum> _values = ImmutableArray<TEnum>.Empty;
    private static ImmutableArray<TEnum> _ignoreObsoleteValues = ImmutableArray<TEnum>.Empty;
    private static readonly ImmutableDictionary<string, TEnum> _stringToEnumDic = ImmutableDictionary<string, TEnum>.Empty;
    
    static EnumBag() {
        var valuesSpan = EnumBag.GetValues(typeof(TEnum));
        if (valuesSpan.IsEmpty) {
            Logger.TraceError($"{nameof(valuesSpan)} is empty");
            return;
        }

        _values = valuesSpan.ToArray().ToArray<TEnum>().ToImmutableArray();
        _ignoreObsoleteValues = _values.Where(enumValue => typeof(TEnum).TryGetFieldInfo(out var info, enumValue.ToString()) && info.IsDefined<ObsoleteAttribute>() == false).ToImmutableArray();
        _stringToEnumDic = _values.ToImmutableDictionary(value => string.Intern(value.ToString()), value => value);
    }

    public static TEnum Get(string stringValue) => _stringToEnumDic.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
    public static ReadOnlySpan<TEnum> GetValues(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues.AsSpan() : _values.AsSpan();
}

public static class EnumUtil {
    
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum => Enum.IsDefined(typeof(TEnum), value);
    public static bool IsDefined<TEnum>(string value) where TEnum : struct, Enum => Enum.IsDefined(typeof(TEnum), value);
    public static bool IsDefined<TEnum>(int value) where TEnum : struct, Enum => Enum.IsDefined(typeof(TEnum), value);

    public static bool IsDefinedAllCase<TEnum>(this string value) where TEnum : struct, Enum {
        foreach (var valueCase in ReturnValueAllCase(value)) {
            if (IsDefined<TEnum>(valueCase)) {
                return true;
            }
        }
        
        return false;
    }
    
    #region [string To enum]

    public static bool TryConvert<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        if (IsDefined<TEnum>(value)) {
            enumValue = Convert<TEnum>(value);
            return true;
        }
        
        enumValue = default;
        return false;
    }
    
    public static bool TryConvertFast<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        if (IsDefined<TEnum>(value)) {
            enumValue = ConvertFast<TEnum>(value);
            return true;
        }

        enumValue = default;
        return false;
    }

    public static TEnum Convert<TEnum>(string value) where TEnum : struct, Enum {
        try {
            return Enum.Parse<TEnum>(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }
    
    public static TEnum ConvertFast<TEnum>(string value) where TEnum : struct, Enum {
        try {
            return EnumBag<TEnum>.Get(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return default;
        }
    }

    public static Enum ConvertFast(Type type, string value) {
        try {
            return EnumBag.Get(type, value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            throw;
        }
    }
    
    public static bool TryConvertAllCase<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        foreach (var valueCase in ReturnValueAllCase(value)) {
            if (TryConvertFast(valueCase, out enumValue)) {
                return true;
            }
        }

        enumValue = default;
        return false;
    }

    public static TEnum ConvertAllCase<TEnum>(string value) where TEnum : struct, Enum {
        try {
            foreach (var valueCase in ReturnValueAllCase(value)) {
                if (IsDefined<TEnum>(valueCase)) { 
                    return ConvertFast<TEnum>(valueCase);
                }
            }
            
            return default;
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return default;
        }
    }

    public static bool TryConvertAllCase(Type enumType, string value, out object enumValue) {
        try {
            if (enumType.IsEnum) {
                foreach (var valueCase in ReturnValueAllCase(value)) {
                    if (Enum.IsDefined(enumType, valueCase)) {
                        enumValue = Enum.Parse(enumType, valueCase); 
                        return true;
                    }
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        enumValue = default;
        return false;
    }
    
    public static object ConvertAllCase(Type enumType, string value) {
        try {
            if (enumType.IsEnum == false) {
                return default;
            }

            foreach (var valueCase in ReturnValueAllCase(value)) {
                if (Enum.IsDefined(enumType, valueCase)) {
                    return Enum.Parse(enumType, valueCase);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return default;
    }
    
    #endregion

    #region [int To enum]
    
    public static bool TryConvert<TEnum>(int value, out TEnum outType) where TEnum : struct, Enum {
        outType = default;
        if (IsDefined<TEnum>(value)) {
            outType = Convert<TEnum>(value);
            return true;
        }

        return false;
    }

    public static bool TryConvertFast<TEnum>(int value, out TEnum outType) where TEnum : struct, Enum {
        if (IsDefined<TEnum>(value)) {
            outType = ConvertFast<TEnum>(value);
            return true;
        }
        
        outType = default;
        return false;
    }

    public static TEnum Convert<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return default;
        }
    }

    public static TEnum ConvertFast<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return ExpressionProvider.GetIntToEnumFunc<TEnum>().Invoke(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return default;
        }
    }

    #endregion

    #region [enum to int]

    public static int Convert<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return System.Convert.ToInt32(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return int.MinValue;
        }
    }
    
    public static int ConvertFast<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return ExpressionProvider.GetEnumToIntFun<TEnum>().Invoke(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert fail || {value} || {ex}");
            return -1;
        }
    }
    
    #endregion

    public static ReadOnlySpan<TEnum> GetValues<TEnum>(bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => ignoreDefault ? EnumBag<TEnum>.GetValues(ignoreObsolete)[1..] : EnumBag<TEnum>.GetValues(ignoreObsolete);
    public static ReadOnlySpan<Enum> GetValues(Type type, bool ignoreDefault = false, bool ignoreObsolete = false) => ignoreDefault ? EnumBag.GetValues(type, ignoreObsolete)[1..] : EnumBag.GetValues(type, ignoreObsolete);

    public static List<TEnum> GetValueList<TEnum>(bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => GetValues<TEnum>(ignoreDefault, ignoreObsolete).ToList();
    
    private static IEnumerable<string> ReturnValueAllCase(string value) {
        yield return value;
        yield return value.GetForceTitleCase();
        yield return value.ToUpper();
        yield return value.ToLower();
    }

    public static bool IsDefault<TEnum>(TEnum value) where TEnum : struct, Enum => value.Equals(default(TEnum));
}

public static class EnumExtension {

    public static sbyte ToSByte(this Enum enumValue) => System.Convert.ToSByte(enumValue);
    public static byte ToByte(this Enum enumValue) => System.Convert.ToByte(enumValue);
    public static short ToInt16(this Enum enumValue) => System.Convert.ToInt16(enumValue);
    public static ushort ToUInt16(this Enum enumValue) => System.Convert.ToUInt16(enumValue);
    public static int ToInt32(this Enum enumValue) => System.Convert.ToInt32(enumValue);
    public static uint ToUInt32(this Enum enumValue) => System.Convert.ToUInt32(enumValue);
    public static long ToInt64(this Enum enumValue) => System.Convert.ToInt64(enumValue);
    public static ulong ToUInt64(this Enum enumValue) => System.Convert.ToUInt64(enumValue);

    public static TEnum Convert<TEnum>(this Enum enumValue) where TEnum : struct, Enum => EnumUtil.ConvertFast<TEnum>(enumValue.ToString());

    public static ReadOnlySpan<TEnum> GetValues<TEnum>(this TEnum _, bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => EnumUtil.GetValues<TEnum>(ignoreDefault, ignoreObsolete);
    public static ReadOnlySpan<Enum> GetValues(Type type, bool ignoreDefault = false, bool ignoreObsolete = false) => EnumUtil.GetValues(type, ignoreDefault, ignoreObsolete);

    public static List<TEnum> GetValueList<TEnum>(this TEnum _, bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => EnumUtil.GetValueList<TEnum>(ignoreDefault, ignoreObsolete);
}