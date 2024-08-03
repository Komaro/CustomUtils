using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public static class CommonExtension {

    private static readonly StringBuilder _stringBuilder = new();

    public static string ToStringAllFields(this object ob, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public) {
        if (_stringBuilder == null) {
            return string.Empty;
        }
        
        var type = ob.GetType();
        _stringBuilder.Clear();
        _stringBuilder.AppendLine(type.Name);
        foreach (var info in type.GetFields(bindingFlags)) {
            var (name, field) = (info.Name, info.GetValue(ob));
            var fieldType = field.GetType();
            if (fieldType.IsArray && field is Array array) {
                _stringBuilder.AppendLine($"{name} || {array.Cast<object>()?.ToStringCollection(", ")}");
            } else if (fieldType.IsGenericCollectionType() && field is ICollection collection) {
                _stringBuilder.AppendLine($"{name} || {collection.Cast<object>().ToStringCollection(", ")}");
            } else {
                _stringBuilder.AppendLine($"{name} || {field}");
            }
        }

        return _stringBuilder.ToString();
    }
    
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

    public static bool IsGenericCollectionType(this Type type) {
        if (type.IsGenericType == false) {
            return false;
        }

        foreach (var interfaceType in type.GetInterfaces()) {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                return true;
            }
        }

        return false;
    }

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
}
