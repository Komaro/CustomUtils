using System;
using System.Collections.Generic;

public static partial class EnumUtil {
    
    public static bool IsDefined<TEnum>(object obj) where TEnum : struct, Enum => Enum.IsDefined(typeof(TEnum), obj);
    // public static bool IsDefined<TEnum>(Enum value) => Enum.IsDefined(typeof(TEnum), value);

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
        try {
            if (IsDefined<TEnum>(value)) {
                enumValue = Convert<TEnum>(value);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        enumValue = default;
        return false;
    }
    
    public static bool TryConvertFast<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        try {
            if (IsDefined<TEnum>(value)) {
                enumValue = ConvertFast<TEnum>(value);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        enumValue = default;
        return false;
    }

    public static TEnum Convert<TEnum>(string value) where TEnum : struct, Enum {
        try {
            return Enum.Parse<TEnum>(value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }
    }
    
    public static TEnum ConvertFast<TEnum>(string value) where TEnum : struct, Enum {
        try {
            return EnumBag<TEnum>.Get(value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }
    }

    public static Enum ConvertFast(Type type, string value) {
        try {
            return EnumBag.Get(type, value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(type, value, ex);
        }
    }
    
    public static bool TryConvertAllCase<TEnum>(string value, out TEnum enumValue) where TEnum : struct, Enum {
        try {
            foreach (var valueCase in ReturnValueAllCase(value)) {
                if (TryConvertFast(valueCase, out enumValue)) {
                    return true;
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
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
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }

        throw new InvalidEnumCastException(typeof(TEnum), value);
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
            if (enumType.IsEnum) {
                foreach (var valueCase in ReturnValueAllCase(value)) {
                    if (Enum.IsDefined(enumType, valueCase)) {
                        return Enum.Parse(enumType, valueCase);
                    }
                }
            }
        } catch (Exception ex) {
            throw new InvalidEnumCastException(enumType, value, ex);
        }
        
        throw new InvalidEnumCastException(enumType, value);
    }
    
    #endregion

    #region [int To enum]
    
    public static bool TryConvert<TEnum>(int value, out TEnum outValue) where TEnum : struct, Enum {
        try {
            if (IsDefined<TEnum>(value)) {
                outValue = Convert<TEnum>(value);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        outValue = default;
        return false;
    }

    public static bool TryConvertFast<TEnum>(int value, out TEnum outValue) where TEnum : struct, Enum {
        try {
            if (IsDefined<TEnum>(value)) {
                outValue = ConvertFast<TEnum>(value);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        outValue = default;
        return false;
    }

    public static TEnum Convert<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }
    }
    
    public static TEnum ConvertFast<TEnum>(int value) where TEnum : struct, Enum {
        try {
            return ExpressionProvider.GetIntToEnumFunc<TEnum>().Invoke(value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }
    }

    #endregion

    #region [enum to int]

    public static int Convert<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return System.Convert.ToInt32(value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
        }
    }
    
    public static int ConvertFast<TEnum>(TEnum value) where TEnum : struct, Enum {
        try {
            return ExpressionProvider.GetEnumToIntFun<TEnum>().Invoke(value);
        } catch (Exception ex) {
            throw new InvalidEnumCastException(typeof(TEnum), value, ex);
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