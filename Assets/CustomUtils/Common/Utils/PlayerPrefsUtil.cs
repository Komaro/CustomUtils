using System;
using Newtonsoft.Json;
using UnityEngine;

public static class PlayerPrefsUtil {
    
    public static bool TryGet(string key, out string value, string defaultValue = "") {
        try {
            value = GetString(key, defaultValue);
            return string.IsNullOrEmpty(value) == false;
        } catch (Exception) {
            value = defaultValue;
        }
        
        return false;
    }

    public static bool TryGet(string key, out bool value, bool defaultValue = false) {
        try {
            value = GetInt(key) > 0;
            return true;
        } catch (Exception) {
            value = defaultValue;
        }

        return false;
    }

    public static bool TryGet(string key, out int value, int defaultValue = 0) {
        try {
            value = GetInt(key);
            return true;
        } catch {
            value = defaultValue;
        }
        
        return false;
    }

    public static bool TryGet(string key, out long value, long defaultValue = 0) {
        if (TryGet(key, out string rawValue) && long.TryParse(rawValue, out value)) {
            return true;
        }
        
        value = defaultValue;
        return false;
    }

    public static bool TryGet(string key, out float value, float defaultValue = 0f) {
        try {
            value = GetFloat(key);
            return true;
        } catch {
            value = defaultValue;
        }

        return false;
    }

    public static bool TryGet(string key, out double value, double defaultValue = 0d) {
        if (TryGet(key, out string rawValue) && double.TryParse(rawValue, out value)) {
            return true;
        }
        
        value = defaultValue;
        return false;
    }
    
    public static bool TryGet<TEnum>(string key, out TEnum value, TEnum defaultValue = default) where TEnum : struct, Enum {
        try {
            if (TryGet(key, out string rawValue) && Enum.TryParse(rawValue, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        value = defaultValue;
        return false;
    }
    
    public static void Set(string key, string value) => SetString(key, value);
    public static void Set(string key, bool value) => SetInt(key, value ? 1 : 0);
    public static void Set(string key, int value) => SetInt(key, value);
    public static void Set(string key, long value) => SetString(key, value.ToString(Constants.Culture.DEFAULT_CULTURE_INFO));
    public static void Set(string key, float value) => SetFloat(key, value);
    public static void Set(string key, double value) => SetString(key, value.ToString(Constants.Culture.DEFAULT_CULTURE_INFO));
    public static void Set<TEnum>(string key, TEnum value) where TEnum : struct, Enum => SetString(key, value.ToString());
    
    public static string GetString(string key, string defaultValue = "") {
        try {
            return PlayerPrefs.GetString(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }

    public static void SetString(string key, string value) {
        try {
            PlayerPrefs.SetString(key, value);
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
            PlayerPrefs.SetInt(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    
    public static float GetFloat(string key, float defaultValue = 0f) {
        try {
            return PlayerPrefs.GetFloat(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return defaultValue;
    }

    public static void SetFloat(string key, float value) {
        try {
            PlayerPrefs.SetFloat(key, value);
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
