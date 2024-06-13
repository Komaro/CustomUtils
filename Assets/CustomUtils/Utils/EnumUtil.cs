using System;
using System.Collections.Generic;
using System.Reflection;

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

    public static bool IsDefinedInt<T>(this int num) where T : struct, Enum => Enum.IsDefined(typeof(T), num);

    public static bool IsDefinedInt<T>(this int num, out T type) where T : struct, Enum {
        type = default;
        if (IsDefinedInt<T>(num)) {
            type = ConvertInt<T>(num);
            return true;
        }

        return false;
    }

    public static T Convert<T>(string type) where T : struct, Enum {
        try {
            return (T)Enum.Parse(typeof(T), type);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {type} || {ex}");
            return default;
        }
    }

    public static T ConvertAllCase<T>(string type) where T : struct, Enum {
        try {
            if (IsDefined<T>(type)) { 
                return Convert<T>(type);
            }

            type = type.ToUpper();
            if (IsDefined<T>(type)) {
                return Convert<T>(type);
            }
            
            type = type.GetForceTitleCase();
            if (IsDefined<T>(type)) {
                return Convert<T>(type);
            }
            
            return default;
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {type} || {ex}");
            return default;
        }
    }

    public static T ConvertInt<T>(int num) where T : struct, Enum {
        try {
            if (Enum.IsDefined(typeof(T), num)) {
                return (T)Enum.ToObject(typeof(T), num);
            }
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {num} || {ex}");
            return default;
        }
        return default;
    }

    public static bool TryGetValue<T>(int num, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefinedInt<T>(num)) {
            outType = ConvertInt<T>(num);
            return true;
        }

        return false;
    }

    public static bool TryGetValue<T>(string type, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(type)) {
            outType = Convert<T>(type);
            return true;
        }

        return false;
    }

    public static bool TryGetValueAllCase<T>(string type, out T outType) where T : struct, Enum {
        outType = default;
        if (IsDefined<T>(type)) {
            outType = Convert<T>(type);
            return true;
        }
        
        type = type.ToUpper();
        if (IsDefined<T>(type)) {
            outType = Convert<T>(type);
            return true;
        }

        type = type.GetForceTitleCase();
        if (IsDefined<T>(type)) {
            outType = Convert<T>(type);
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