using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Logger {
    
    // Log
    public static void Log(string text) => Debug.Log(text);
    public static void Log(string text, Color color) => Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
    public static void Log(string format, params object[] args) => Log(string.Format(format, args));
    public static void Log(string format, Color color, params object[] args) => Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{format}</color>", args);
    public static void Log(object obj) => Log(obj?.ToString() ?? string.Empty);
    public static void Log(object obj, Color color) => Log(obj?.ToString() ?? string.Empty, color);

    // Warning
    public static void Warning(string text) => Debug.LogWarning(text);
    public static void Warning(string text, Color color) => Warning($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
    public static void Warning(string format, params object[] args) => Warning(string.Format(format, args));
    public static void Warning(string format, Color color, params object[] args) => Warning($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{format}</color>", args);
    public static void Warning(object obj) => Warning(obj?.ToString() ?? string.Empty);
    public static void Warning(object obj, Color color) => Warning(obj?.ToString() ?? string.Empty, color);

    // Error
    public static void Error(string text) => Debug.LogError(text);
    public static void Error(string text, Color color) => Error($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
    public static void Error(string format, params object[] args) => Error(string.Format(format, args));
    public static void Error(string format, Color color, params object[] args) => Error($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{format}</color>", args);
    public static void Error(object obj) => Error(obj?.ToString() ?? string.Empty);
    public static void Error(object obj, Color color) => Error(obj?.ToString() ?? string.Empty, color);


    // TraceLog
    public static void TraceLog(string text) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name}.{caller?.GetMethod()?.Name}] {text}");
    }

    public static void TraceLog(string text, Color color) {
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

    public static void TraceLog(object obj) {
        var caller = GetCaller();
        Log($"[{caller?.GetMethod()?.ReflectedType?.Name ?? string.Empty}.{caller?.GetMethod()?.Name ?? string.Empty}] {obj?.ToString() ?? string.Empty}");
    }

    public static void TraceLog(object obj, Color color) => TraceLog(obj?.ToString() ?? string.Empty, color);


    // TraceWarning
    public static void TraceWarning(string text) {
        var caller = GetCaller();
        Warning($"[{caller?.GetMethod()?.ReflectedType?.Name}.{caller?.GetMethod()?.Name}] {text}");
    }


    // TraceError
    public static void TraceError(string text) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {text}");
    }

    public static void TraceError(string text, Color color) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {text}", color);
    }

    public static void TraceError(string format, params object[] args) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {string.Format(format, args)}");
    }

    public static void TraceError(string format, Color color, params object[] args) {
        var caller = GetCaller();
        Error($"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {string.Format(format, args)}", color);
    }

    public static void TraceError(object obj) => TraceError(obj?.ToString() ?? string.Empty);
    public static void TraceError(object obj, Color color) => TraceError(obj?.ToString() ?? string.Empty, color);

    public static void SimpleTraceError(string text, [CallerFilePath] string caller = null) => Error($"[{caller}] {text}");

    private static StackFrame GetCaller() => new StackTrace().GetFrame(2);

    // TODO. Need Test
    public static void TraceErrorExpensive(string text) {
        var caller = GetCaller();
        Error(caller?.GetMethod().DeclaringType == null
            ? $"[{caller?.GetMethod().ReflectedType?.Name}.{caller?.GetMethod().Name}] {text}"
            : $"[{GetCallerMethod(caller?.GetMethod().DeclaringType)?.FullName}.{caller?.GetMethod()?.Name}] {text}");
    }

    private static Type GetCallerMethod(Type type) => type.DeclaringType == null ? type : GetCallerMethod(type.DeclaringType);
}