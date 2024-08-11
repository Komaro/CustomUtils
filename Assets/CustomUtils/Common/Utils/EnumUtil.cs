using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

    public static TEnum Convert<TEnum>(string value) where TEnum : struct, Enum {
        try {
            return Enum.Parse<TEnum>(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    public static TEnum ConvertAllCase<TEnum>(string value) where TEnum : struct, Enum {
        try {
            foreach (var valueCase in ReturnValueAllCase(value)) {
                if (IsDefined<TEnum>(valueCase)) { 
                    return Convert<TEnum>(valueCase);
                }
            }
            
            return default;
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
     
    public static TEnum Convert<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
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
    
    public static int Convert<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return System.Convert.ToInt32(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return -1;
        }
    }
    
    public static bool TryConvert<TEnum>(int value, out TEnum outType) where TEnum : struct, Enum {
        outType = default;
        if (IsDefined<TEnum>(value)) {
            outType = Convert<TEnum>(value);
            return true;
        }

        return false;
    }

    public static bool TryConvert<TEnum>(string value, out TEnum outType) where TEnum : struct, Enum {
        outType = default;
        if (IsDefined<TEnum>(value)) {
            outType = Convert<TEnum>(value);
            return true;
        }

        return false;
    }

    public static bool TryConvertAllCase<TEnum>(string value, out TEnum outType) where TEnum : struct, Enum {
        outType = default;
        foreach (var valueCase in ReturnValueAllCase(value)) {
            if (TryConvert(valueCase, out outType)) {
                return true;
            }
        }

        return false;
    }

    // public static TEnum[] GetValues<TEnum>(bool ignoreDefault = false, bool isIgnoreObsolete = false) {
    //     var values = Enum.GetValues(typeof(TEnum));
    // }

    public static List<TEnum> GetValueList<TEnum>(bool isIgnoreDefault = false, bool isIgnoreObsolete = false) where TEnum : struct, Enum {
        var type = typeof(TEnum);
        var list = new List<TEnum>();
        foreach (TEnum item in Enum.GetValues(type)) {
            if (isIgnoreObsolete && type.TryGetField(Enum.GetName(type, item), out var info) && info.IsDefined<ObsoleteAttribute>()) {
                continue;
            }
            
            list.Add(item);
        }

        if (isIgnoreDefault) {
            list.RemoveAt(0);
        }
        
        return list;
    }

    private static IEnumerable<string> ReturnValueAllCase(string value) {
        yield return value;
        yield return value.GetForceTitleCase();
        yield return value.ToUpper();
        yield return value.ToLower();
    }

    public static bool IsDefault<TEnum>(TEnum value) where TEnum : struct, Enum => value.Equals(default(TEnum));
}

public static class EnumExtension {

    private static ConcurrentDictionary<Type, Array> _valuesDic = new();

    public static TEnum[] GetValues<TEnum>(this TEnum type, bool ignoreDefault = false, bool isIgnoreObsolete = false) where TEnum : struct, Enum {
        var enumType = typeof(TEnum);
        if (_valuesDic.TryGetValue(enumType, out var array)) {
            return array.Cast<TEnum>().ToArray();
        }

        if (EnumUtil.IsDefined(type)) {
            var enumerable = Enum.GetValues(enumType).Cast<TEnum>();
            var values = enumerable.Where((value, index) => {
                if (ignoreDefault && index == 0) {
                    return false;
                }

                if (isIgnoreObsolete && enumType.TryGetField(value.ToString(), out var info) && info.IsDefined<ObsoleteAttribute>()) {
                    return false;
                }

                return true;
            }).ToArray();
            
            _valuesDic.AutoAdd(enumType, values);
            return values;
        }

        return Array.Empty<TEnum>();
    }
}