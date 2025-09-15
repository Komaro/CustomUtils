using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;

public static class CommonExtension {


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfNull<T>(this T instance) where T : class => instance ?? throw new ArgumentNullException(typeof(T).GetCleanFullName());
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfNull<T>(this T instance, string name) where T : class => instance ?? throw new ArgumentNullException(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ThrowIfNull(this string instance) => string.IsNullOrEmpty(instance) ? throw new ArgumentNullException(nameof(String)) : instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ThrowIfNull(this string instance, string name) => string.IsNullOrEmpty(instance) ? throw new ArgumentNullException(name) : instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfUnexpectedNull<T>(this T instance) where T : class => instance ?? throw new NullReferenceException<T>(typeof(T).GetCleanFullName());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfUnexpectedNull<T>(this T instance, string name) where T : class => instance ?? throw new NullReferenceException<T>(name);

    public static string ToStringAllFields(this object ob, string prefix = "", bool ignoreRootName = false, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public) {
        if (ob == null) {
            return $"{nameof(ob)} is null";
        }

        StringUtil.StringBuilderPool.Get(out var builder);
        try {
            var type = ob.GetType();
            if (ignoreRootName == false) {
                builder.Append(type.GetNameWithGenericArguments());
            }
        
            if (type.IsClass) {
                builder.AppendLine(" (Class)");
            } else if (type.IsStruct()) {
                builder.AppendLine(" (Struct)");
            }

            foreach (var (name, value) in type.GetAllDataMemberNameWithValue(ob, bindingFlags)) {
                if (value == null) {
                    builder.AppendLine($"{prefix} [{name}] null");
                    continue;
                }
            
                var memberType = value.GetType();
                builder.Append($"{prefix} [{memberType.GetNameWithGenericArguments()}] ");
                if (memberType.IsArray && value is Array array) {
                    builder.AppendLine($"{name} || {array.Cast<object>()?.ToStringCollection(", ")}");
                } else if (memberType.IsGenericCollectionType() && value is ICollection { Count: > 0 } collection) {
                    builder.AppendLine($"{name} || {collection.Cast<object>().ToStringCollection(", ")}");
                } else if (memberType.IsEnum == false && memberType.IsStruct()) {
                    builder.AppendLine($"{name} {value.ToStringAllFields("\t", true, bindingFlags)}");
                } else {
                    builder.AppendLine($"{name} || {value}");
                }
            }
        
            return builder.ToString();
        } catch (Exception ex) {
            Logger.TraceLog(ex);
            Logger.TraceError(builder.ToString());
        } finally {
            StringUtil.StringBuilderPool.Release(builder);
        }

        return string.Empty;
    }

    private static string GetNameWithGenericArguments(this Type type) => type.IsGenericType ? $"{type.Name}<{type.GenericTypeArguments.ToStringCollection(genericType => genericType.Name, ", ")}>" : type.Name;
    
    public static string GetString(this Memory<byte> memory, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetString(memory.Span),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetString(memory.Span),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetString(memory.Span),
        _ => Encoding.UTF8.GetString(memory.Span)
    };
    
    public static string GetString(this ref Span<byte> span, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetString(span),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetString(span),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetString(span),
        _ => Encoding.UTF8.GetString(span)
    };

    public static string GetString(this byte[] bytes, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetString(bytes),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetString(bytes),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetString(bytes),
        _ => Encoding.UTF8.GetString(bytes)
    };

    public static string GetString(this byte[] bytes, int length, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetString(bytes, 0, length),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetString(bytes, 0, length),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetString(bytes, 0, length),
        _ => Encoding.UTF8.GetString(bytes)
    };

    public static string GetRawString(this byte[] bytes) => Convert.ToBase64String(bytes);

    public static bool TryMatch(this Regex regex, string text, out Match match) {
        match = regex.Match(text);
        return match.Success;
    }

    public static float GetPreferredWidth(this TextGenerator textGenerator, string text, TextGenerationSettings settings) => textGenerator.GetPreferredWidth(text, settings);
    public static float GetPreferredHeight(this TextGenerator textGenerator, string text, TextGenerationSettings settings) => textGenerator.GetPreferredHeight(text, settings);

    public static ulong GetContentLength(this UnityWebRequestAsyncOperation operation) => operation.webRequest?.GetContentLength() ?? 0u;
    public static ulong GetContentLength(this UnityWebRequest request) => ulong.TryParse(request.GetResponseHeader(HttpResponseHeader.ContentLength.GetName()), out var contentLength) ? contentLength : 0u;

    // 필요한 경우 갱신
    public static string GetName(this HttpResponseHeader header) => header switch {
        HttpResponseHeader.ContentLength => "Content-Length",
        HttpResponseHeader.ContentType => "Content-Type",
        _ => header.ToString()
    };

    public static byte[] ToBytes(this int value) {
        Span<byte> bytes = stackalloc byte[4];
        MemoryMarshal.Write(bytes, ref value);
        return bytes.ToArray();
    }

    public static async Task<IEnumerable<T>> ToEnumerable<T>(this IAsyncEnumerable<T> asyncEnumerable) {
        var list = new List<T>();
        await foreach (var value in asyncEnumerable) {
            list.Add(value);
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryInvoke<TResult>([NotNull]this Func<TResult> func, out TResult result) => func == null ? throw new ArgumentNullException(nameof(func)) : (result = func.Invoke()) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryInvoke<T, TResult>(this Func<T, TResult> func, T arg, out TResult result) => func == null ? throw new ArgumentNullException(nameof(func)) : (result = func.Invoke(arg)) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryInvoke<T1, T2, TResult>(this Func<T1, T2, TResult> func, T1 arg1, T2 arg2, out TResult result) => func == null ? throw new ArgumentNullException(nameof(func)) : (result = func.Invoke(arg1, arg2)) != null;
}
