using System;
using UnityEngine;

public static class PlayerPrefsUtil {

    public static bool TryGet(string key, out string value) {
        try {
            value = GetString(key);
            return string.IsNullOrEmpty(value) == false;
        } catch (Exception) {
            value = string.Empty;
        }
        
        return false;
    }

    public static bool TryGet(string key, out bool value) {
        try {
            value = GetInt(key) > 0;
            return true;
        } catch (Exception) {
            value = false;
        }

        return false;
    }

    public static bool TryGet(string key, out int value) {
        try {
            value = GetInt(key);
            return true;
        } catch {
            value = 0;
        }
        
        return false;
    }

    public static bool TryGet(string key, out long value) {
        if (TryGet(key, out string rawValue) && long.TryParse(rawValue, out value)) {
            return true;
        }
        
        value = 0;
        return false;
    }

    public static bool TryGet(string key, out float value) {
        try {
            value = GetFloat(key);
            return true;
        } catch {
            value = 0f;
        }

        return false;
    }

    public static bool TryGet(string key, out double value) {
        if (TryGet(key, out string rawValue) && double.TryParse(rawValue, out value)) {
            return true;
        }
        
        value = 0d;
        return false;
    }

    public static bool TryGet<T>(string key, out T value) where T : struct, Enum {
        try {
            if (TryGet(key, out string rawValue) && Enum.TryParse(rawValue, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        value = default;
        return false;
    }
    
    public static void Set(string key, string value) => SetString(key, value);
    public static void Set(string key, bool value) => SetInt(key, value ? 1 : 0);
    public static void Set(string key, int value) => SetInt(key, value);
    public static void Set(string key, long value) => SetString(key, value.ToString(Constants.Culture.DEFAULT_CULTURE_INFO));
    public static void Set(string key, float value) => SetFloat(key, value);
    public static void Set(string key, double value) => SetString(key, value.ToString(Constants.Culture.DEFAULT_CULTURE_INFO));
    public static void Set<T>(string key, T value) where T : struct, Enum => SetString(key, value.ToString());

    public static string GetString(string key, string defaultValue = "") {
        try {
            return PlayerPrefs.GetString($"String_{key}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }

    public static void SetString(string key, string value) {
        try {
            PlayerPrefs.SetString($"String_{key}", value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static int GetInt(string key, int defaultValue = 0) {
        try { 
            return PlayerPrefs.GetInt(key, defaultValue);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return defaultValue;
    }
    
    public static void SetInt(string key, int value) {
        try {
            PlayerPrefs.SetInt($"Int_{key}", value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static float GetFloat(string key, float defaultValue = 0f) {
        try {
            return PlayerPrefs.GetFloat($"Float_{key}");
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return defaultValue;
    }

    public static void SetFloat(string key, float value) {
        try {
            PlayerPrefs.SetFloat($"Float_{key}", value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void Delete(string key) {
        try {
            PlayerPrefs.DeleteKey(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void Clear() {
        try {
            PlayerPrefs.DeleteAll();
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static bool ContainsKey(string key) {
        try {
            return PlayerPrefs.HasKey(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return false;
    }
}
