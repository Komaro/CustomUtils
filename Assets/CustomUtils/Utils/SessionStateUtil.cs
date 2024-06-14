using System;
using UnityEditor;

public static class SessionStateUtil {
    
    public static bool TryGet(string key, out string value, string defaultValue = "") {
        try {
            value = GetString(key, defaultValue);
            return true;
        } catch (Exception) {
            value = defaultValue;
        }

        return false;
    }

    public static bool TryGet(string key, out bool value, bool defaultValue = false) {
        try {
            value = GetBool(key, defaultValue);
            return true;
        } catch (Exception) {
            value = defaultValue;
        }
        
        return false;
    }
    
    public static bool TryGet(string key, out int value, int defaultValue = 0) {
        try {
            value = GetInt(key, defaultValue);
            return true;
        } catch (Exception) {
            value = defaultValue;
        }
        
        return false;
    }

    public static bool TryGet(string key, out float value, float defaultValue = 0f) {
        try {
            value = GetFloat(key, defaultValue);
            return true;
        } catch (Exception) {
            value = defaultValue;
        }

        return false;
    }

    public static void Set(string key, string value) => SetString(key, value);
    public static void Set(string key, bool value) => SetBool(key, value);
    public static void Set(string key, int value) => SetInt(key, value);
    public static void Set(string key, float value) => SetFloat(key, value);

    public static string GetString(string key, string defaultValue = "") {
        try {
            return SessionState.GetString(key, defaultValue);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }
    
    public static void SetString(string key, string value) {
        try {
            SessionState.SetString(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static bool GetBool(string key, bool defaultValue = false) {
        try {
            return SessionState.GetBool(key, defaultValue);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }
    
    public static void SetBool(string key, bool value) {
        try {
            SessionState.SetBool(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static int GetInt(string key, int defaultValue = 0) {
        try {
            return SessionState.GetInt(key, defaultValue);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }
    
    public static void SetInt(string key, int value) {
        try {
            SessionState.SetInt(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static float GetFloat(string key, float defaultValue = 0f) {
        try {
            return SessionState.GetFloat(key, defaultValue);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return defaultValue;
    }
    
    public static void SetFloat(string key, float value) {
        try {
            SessionState.SetFloat(key, value);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
}