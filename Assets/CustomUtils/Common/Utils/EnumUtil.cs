using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

internal static class EnumBag<TEnum> where TEnum : struct, Enum {

    private static readonly TEnum[] _values;
    private static readonly TEnum[] _ignoreObsoleteValues;
    private static readonly ConcurrentDictionary<string, TEnum> _stringToEnumDic = new();

    static EnumBag() {
        var enumType = typeof(TEnum);
        if (Enum.GetValues(enumType) is TEnum[] enumValues) {
            _values = enumValues;
            _ignoreObsoleteValues = _values.Where(enumValue => enumType.TryGetFieldInfo(out var info, enumValue.ToString()) && info.IsDefined<ObsoleteAttribute>() == false).ToArray();
            foreach (var enumValue in _values) {
                _stringToEnumDic.TryAdd(enumValue.ToString(), enumValue);
            }
        }
    }

    public static TEnum Get(string stringValue) => _stringToEnumDic.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
    public static TEnum[] GetValues(bool ignoreObsolete = false) => ignoreObsolete ? _ignoreObsoleteValues : _values;
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

    public static bool TryConvertAllCase<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        foreach (var valueCase in ReturnValueAllCase(value)) {
            if (TryConvertFast(valueCase, out enumValue)) {
                return true;
            }
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
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
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
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
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
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    public static TEnum ConvertFast<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return LambdaExpressionProvider.GetIntToEnumFunc<TEnum>().Invoke(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    #endregion

    #region [enum to int]

    public static int Convert<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return System.Convert.ToInt32(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return int.MinValue;
        }
    }
    
    public static int ConvertFast<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return LambdaExpressionProvider.GetEnumToIntFun<TEnum>().Invoke(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return -1;
        }
    }
    
    #endregion

    public static IEnumerable<TEnum> GetValues<TEnum>(bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => EnumBag<TEnum>.GetValues(ignoreObsolete).Where((_, index) => ignoreDefault == false || index != 0);
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

    public static short ConvertShort(this Enum enumValue) => enumValue.GetTypeCode() == TypeCode.Int16 ? System.Convert.ToInt16(enumValue) : (short) -1;
    public static int ConvertInt(this Enum enumValue) => enumValue.GetTypeCode() == TypeCode.Int32 ? System.Convert.ToInt32(enumValue) : -1;
    
    public static TEnum Convert<TEnum>(this Enum enumValue) where TEnum : struct, Enum => EnumUtil.ConvertFast<TEnum>(enumValue.ToString());

    public static TEnum[] GetValues<TEnum>(this TEnum _, bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum {
        var values = EnumBag<TEnum>.GetValues(ignoreObsolete);
        if (ignoreDefault) {
            values.AsSpan().Slice(1, values.Length - 1).ToArray();
        }
        
        return values;
    }

    public static List<TEnum> GetValueList<TEnum>(this TEnum _, bool ignoreDefault = false, bool ignoreObsolete = false) where TEnum : struct, Enum => EnumUtil.GetValueList<TEnum>(ignoreDefault, ignoreObsolete);
}