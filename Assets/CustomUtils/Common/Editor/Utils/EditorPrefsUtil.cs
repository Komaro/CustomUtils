using System;
using UnityEditor;

public static class EditorPrefsUtil {

    public const string ASSET_PIPELINE_AUTO_REFRESH = "kAutoRefreshMode";

    public static bool TryGet(string key, out string value, string defaultValue = "") {
        try {
            value = GetString(key, defaultValue);
            return string.IsNullOrEmpty(value) == false;
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

    public static bool TryGet<T>(string key, out T value, T defaultValue = default) where T : struct, Enum {
        try {
            if (TryGet(key, out int rawValue) && EnumUtil.TryConvert<T>(rawValue, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        value = defaultValue;
        return false;
    }

    public static void Set(string key, string value) => SetString(key, value);
    public static void Set(string key, int value) => SetInt(key, value);
    public static void Set<T>(string key, T value) where T : struct, Enum => SetInt(key, EnumUtil.Convert(value));

    public static string GetString(string key, string defaultValue = "") {
        try {
            return EditorPrefs.GetString(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }

    public static void SetString(string key, string value) {
        try {
            EditorPrefs.SetString(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static int GetInt(string key, int defaultValue = 0) {
        try {
            return EditorPrefs.GetInt(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }
    
    public static void SetInt(string key, int value) {
        try {
            EditorPrefs.SetInt(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
}