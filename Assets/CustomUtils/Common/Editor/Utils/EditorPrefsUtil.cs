using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
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

    public static bool TryGet<TEnum>(string key, out TEnum value, TEnum defaultValue = default) where TEnum : struct, Enum {
        try {
            if (TryGet(key, out int rawValue) && EnumUtil.TryConvert<TEnum>(rawValue, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        value = defaultValue;
        return false;
    }

    public static bool TryGet<T>(string key, out HashSet<T> value) {
        try {
            if (TryGet(key, out string text) && JsonUtil.TryDeserialize(text, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        value = CollectionUtil.HashSet.Empty<T>();
        return false;
    }

    public static bool TryGet<T>(string key, out T[] value) {
        try {
            if (TryGet(key, out string text) && JsonUtil.TryDeserialize(text, out value)) {
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        value = Array.Empty<T>();
        return false;
    }

    public static void Set(string key, string value) => SetString(key, value);
    public static void Set(string key, int value) => SetInt(key, value);
    public static void Set(string key, bool value) => SetBool(key, value);
    public static void Set<TEnum>(string key, TEnum value) where TEnum : struct, Enum => SetInt(key, EnumUtil.Convert(value));
    
    public static void Set<T>(string key, IEnumerable<T> value) {
        if (JsonUtil.TrySerialize(value, out var json)) {
            SetString(key, json);
        }
    }

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

    public static bool GetBool(string key, bool defaultValue = false) {
        try {
            return EditorPrefs.GetBool(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }

    public static void SetBool(string key, bool value) {
        try {
            EditorPrefs.SetBool(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void Delete(string key) {
        try {
            EditorPrefs.DeleteKey(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void Clear() {
        try {
            EditorPrefs.DeleteAll();
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static bool ContainsKey(string key) {
        try {
            return EditorPrefs.HasKey(key);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return false;
    }
}