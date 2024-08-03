using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public static class EnumUtil {
    
    private static bool IsDefined<T>(T type) where T : struct, Enum => Enum.IsDefined(typeof(T), type);
    private static bool IsDefined<T>(string type) where T : struct, Enum => Enum.IsDefined(typeof(T), type);
    
    public static bool IsDefined<T>(this int value) where T : struct, Enum => Enum.IsDefined(typeof(T), value);

    public static bool IsDefined<T>(this int value, out T type) where T : struct, Enum {
        type = default;
        if (IsDefined<T>(value)) {
            type = Convert<T>(value);
            return true;
        }

        return false;
    }

    public static bool IsDefinedAllCase<T>(this string type) where T : struct, Enum {
        if (IsDefined<T>(type)) {
            return true;
        }

        if (IsDefined<T>(type.ToUpper())) {
            return true;
        }

        if (IsDefined<T>(type.ToLower())) {
            return true;
        }

        if (IsDefined<T>(type.GetForceTitleCase())) {
            return true;
        }

        return false;
    }

    public static T Convert<T>(string value) where T : struct, Enum {
        try {
            return (T)Enum.Parse(typeof(T), value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    public static T ConvertAllCase<T>(string value) where T : struct, Enum {
        try {
            foreach (var valueCase in ReturnValueAllCase(value)) {
                if (IsDefined<T>(valueCase)) { 
                    return Convert<T>(valueCase);
                }
            }
            
            return default;
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    public static T Convert<T>(int value) where T : struct, Enum {
        try {
            if (Enum.IsDefined(typeof(T), value)) {
                return (T)Enum.ToObject(typeof(T), value);
            }
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
        return default;
    }
    
    public static int Convert<T>(T value) where T : struct, Enum {
        try {
            return System.Convert.ToInt32(value);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return -1;
        }
    }
    
    public static bool TryConvert<T>(int value, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
        }

        return false;
    }

    public static bool TryConvert<T>(string value, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
        }

        return false;
    }

    public static bool TryConvertAllCase<T>(string value, out T outType) where T : struct, Enum {
        outType = default;
        foreach (var valueCase in ReturnValueAllCase(value)) {
            if (TryConvert<T>(valueCase, out outType)) {
                return true;
            }
        }

        return false;
    }

    public static List<T> GetValues<T>(bool isIgnoreDefault = false, bool isIgnoreObsolete = false) where T : struct, Enum {
        var type = typeof(T);
        var list = new List<T>();
        if (type.IsEnum) {
            foreach (T item in Enum.GetValues(typeof(T))) {
                if (isIgnoreObsolete && type.TryGetField(Enum.GetName(type, item), out var info) && info.IsDefined<ObsoleteAttribute>()) {
                    continue;
                }
                
                list.Add(item);
            }

            if (isIgnoreDefault) {
                list.RemoveAt(0);
            }
        }

        return list;
    }
    
    private static IEnumerable<string> ReturnValueAllCase(string value) {
        yield return value;
        yield return value.ToUpper();
        yield return value.GetForceTitleCase();
    }

    public static bool IsDefault<T>(T type) where T : struct, Enum => type.Equals(default(T));
}