using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SystemColor = System.Drawing.Color;

public static class Logger {
    
    #region [Log]
    
    public static void Log(string text) => Debug.Log(text);
    public static void Log(string text, Color color) => Debug.Log($"<color={color.ToHex()}>{text}</color>");
    public static void Log(string text, SystemColor color) => Debug.Log($"<color={color.ToHex()}>{text}</color>");
    
    public static void Log(string format, params object[] args) => Debug.Log(string.Format(format, args));
    public static void Log(string format, Color color, params object[] args) => Log($"<color={color.ToHex()}>{format}</color>", args);
    public static void Log(string format, SystemColor color, params object[] args) => Log($"<color={color.ToHex()}>{format}</color>", args);
    
    public static void Log(object obj) => Debug.Log(obj?.ToString() ?? string.Empty);
    public static void Log(object obj, Color color) => Log(obj?.ToString() ?? string.Empty, color);
    public static void Log(object obj, SystemColor color) => Log(obj?.ToString() ?? string.Empty, color);

    #endregion

    #region [Warning]
    
    public static void Warning(string text) => Debug.LogWarning(text);
    public static void Warning(string format, params object[] args) => Debug.LogWarning(string.Format(format, args));
    public static void Warning(object obj) => Debug.LogWarning(obj?.ToString() ?? string.Empty);

    #endregion

    #region [Error]
    
    public static void Error(string text) => Debug.LogError(text);
    public static void Error(string format, params object[] args) => Debug.LogError(string.Format(format, args));
    public static void Error(object obj) => Debug.LogError(obj?.ToString() ?? string.Empty);

    #endregion

    #region [TraceLog]
    
    public static void TraceLog(string text) {
        var caller = GetCaller(); 
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name}.{caller?.GetMethod()?.Name}] {text}");
    }

    public static void TraceLog(string text, Color color) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {text}", color);
    }
    
    public static void TraceLog(string text, SystemColor color) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {text}", color);
    }

    public static void TraceLog(string format, params object[] args) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {string.Format(format, args)}");
    }

    public static void TraceLog(string format, Color color, params object[] args) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {string.Format(format, args)}", color);
    }
    
    public static void TraceLog(string format, SystemColor color, params object[] args) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {string.Format(format, args)}", color);
    }

    public static void TraceLog(object obj) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {obj?.ToString() ?? string.Empty}");
    }

    public static void TraceLog(object obj, Color color) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {obj?.ToString() ?? string.Empty}", color);
    }

    public static void TraceLog(object obj, SystemColor color) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {obj?.ToString() ?? string.Empty}", color);
    }

    public static void SimpleTraceLog(string text, [CallerFilePath] string path = null, [CallerMemberName] string method = null, [CallerLineNumber] int line = 0) => Log($"[{Path.GetFileNameWithoutExtension(path)}.{method}.{line}] {text}");

    #endregion

    #region [TraceWarning]
    
    public static void TraceWarning(string text) {
        var caller = GetCaller();
        Warning($"[{caller?.GetMethod()?.ReflectedType?.Name}.{caller?.GetMethod()?.Name}] {text}");
    }
    
    public static void TraceWarning(string format, params object[] args) {
        var caller = GetCaller();
        Warning($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {string.Format(format, args)}");
    }

    public static void TraceWarning(object obj) {
        var caller = GetCaller();
        Warning($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {obj?.ToString() ?? string.Empty}");
    }
    
    public static void SimpleTraceWarning(string text, [CallerFilePath] string path = null, [CallerMemberName] string method = null, [CallerLineNumber] int line = 0) => Warning($"[{Path.GetFileNameWithoutExtension(path)}.{method}.{line}] {text}");
    
    #endregion

    #region [TraceError]
    
    public static void TraceError(string text) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {text}");
    }

    public static void TraceError(string format, params object[] args) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {string.Format(format, args)}");
    }

    public static void TraceError(object obj) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {obj?.ToString() ?? string.Empty}");
    }

    public static void SimpleTraceError(string text, [CallerFilePath] string path = null, [CallerMemberName] string method = null, [CallerLineNumber] int line = 0) => Error($"[{Path.GetFileNameWithoutExtension(path)}.{method}.{line}] {text}");
    
    #endregion
    
    private static StackFrame GetCaller() => new StackTrace().GetFrame(2);

    public static void TraceErrorExpensive(string text) {
        var caller = GetCaller();
        Error(caller?.GetMethod().DeclaringType == null
            ? $"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {text}"
            : $"[{GetCallerMethod(caller?.GetMethod().DeclaringType)?.FullName}.{caller?.GetMethod()?.Name}] {text}");
    }

    private static Type GetCallerMethod(Type type) => type.DeclaringType == null ? type : GetCallerMethod(type.DeclaringType);
}