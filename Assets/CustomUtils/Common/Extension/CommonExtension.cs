using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;

public static class CommonExtension {

    private static readonly ObjectPool<StringBuilder> _stringBuilderPool = new(() => new StringBuilder(), builder => builder.Clear());

    public static string ToStringAllFields(this object ob, string prefix = "", BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public) {
        if (_stringBuilderPool == null) {
            return string.Empty;
        }

        var stringBuilder = _stringBuilderPool.Get();
        var type = ob.GetType();
        stringBuilder.Append($"{type.Name}");
        if (type.IsClass) {
            stringBuilder.AppendLine(" (Class)");
        } else if (type.IsStruct()) {
            stringBuilder.AppendLine(" (Struct)");
        }

        foreach (var (name, value) in type.GetAllDataMemberNameWithValue(ob, bindingFlags)) {
            var memberType = value.GetType();
            stringBuilder.Append($"{prefix} [{memberType.Name}] ");
            if (memberType.IsArray && value is Array array) {
                stringBuilder.AppendLine($"{name} || {array.Cast<object>()?.ToStringCollection(", ")}");
            } else if (memberType.IsGenericCollectionType() && value is ICollection collection) {
                stringBuilder.AppendLine($"{name} || {collection.Cast<object>().ToStringCollection(", ")}");
            } else if (memberType.IsEnum == false && memberType.IsStruct()) {
                stringBuilder.AppendLine($"{value.ToStringAllFields("\t", bindingFlags)}");
            } else {
                stringBuilder.AppendLine($"{name} || {value}");
            }
        }

        return stringBuilder.ToString();
    }

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
}
