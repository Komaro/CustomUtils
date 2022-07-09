using System;
using System.Collections.Generic;

public static class EnumExtension
{
    public static bool IsDefined<T>(this T type) => Enum.IsDefined(typeof(T), type);
    public static bool IsDefined<T>(this string type) => Enum.IsDefined(typeof(T), type);

    public static bool IsDefinedAllCase<T>(this string type) {
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

    public static bool IsDefinedInt<T>(this int num) => Enum.IsDefined(typeof(T), num);

    public static bool IsDefinedInt<T>(this int num, out T type) {
        type = default;
        if (IsDefinedInt<T>(num)) {
            type = ConvertInt<T>(num);
            return true;
        }

        return false;
    }

    public static T Convert<T>(this string type)
    {
        try {
            return (T)Enum.Parse(typeof(T), type);
        } catch (Exception ex) {
            Logger.TraceError($"Convert Fail || {type} || {ex}");
            return default;
        }
    }

    public static T ConvertAllCase<T>(this string type) {
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

    public static T ConvertInt<T>(int num) {
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

    public static bool TryGetValue<T>(int num, out T outType) {
        outType = default;
        if (IsDefinedInt<T>(num)) {
            outType = ConvertInt<T>(num);
            return true;
        }

        return false;
    }

    public static bool TryGetValue<T>(string type, out T outType) {
        outType = default;
        if (IsDefined<T>(type)) {
            outType = Convert<T>(type);
            return true;
        }

        return false;
    }

    public static bool TryGetValueAllCase<T>(string type, out T outType) {
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

    public static bool IsDefault<T>(T type) => type.Equals(default(T));
}