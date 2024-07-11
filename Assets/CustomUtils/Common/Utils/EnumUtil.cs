using System;
using System.Collections.Generic;

public static class EnumUtil {
    
    private static bool IsDefined<T>(T type) where T : struct, Enum => Enum.IsDefined(typeof(T), type);
    private static bool IsDefined<T>(string type) where T : struct, Enum => Enum.IsDefined(typeof(T), type);

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

    public static bool IsDefinedInt<T>(this int value) where T : struct, Enum => Enum.IsDefined(typeof(T), value);

    public static bool IsDefinedInt<T>(this int value, out T type) where T : struct, Enum {
        type = default;
        if (IsDefinedInt<T>(value)) {
            type = ConvertInt<T>(value);
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
            if (IsDefined<T>(value)) { 
                return Convert<T>(value);
            }

            value = value.ToUpper();
            if (IsDefined<T>(value)) {
                return Convert<T>(value);
            }
            
            value = value.GetForceTitleCase();
            if (IsDefined<T>(value)) {
                return Convert<T>(value);
            }
            
            return default;
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {value} || {ex}");
            return default;
        }
    }

    public static T ConvertInt<T>(int value) where T : struct, Enum {
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

    public static bool TryGetValue<T>(int value, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefinedInt<T>(value)) {
            outType = ConvertInt<T>(value);
            return true;
        }

        return false;
    }

    public static bool TryGetValue<T>(string value, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
        }

        return false;
    }

    public static bool TryGetValueAllCase<T>(string value, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
        }
        
        value = value.ToUpper();
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
        }

        value = value.GetForceTitleCase();
        if (IsDefined<T>(value)) {
            outType = Convert<T>(value);
            return true;
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

    public static bool IsDefault<T>(T type) where T : struct, Enum => type.Equals(default(T));
}