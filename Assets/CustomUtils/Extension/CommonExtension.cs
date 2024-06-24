using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

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

    public static IEnumerator CloneEnumerator(this ICollection collection) => new ArrayList(collection).GetEnumerator();
    
    public static float GetPreferredWidth(this TextGenerator textGenerator, string text, GUIStyle style) => textGenerator.GetPreferredWidth(text, style.ToTextGenerationSettings());

    public static TextGenerationSettings ToTextGenerationSettings(this GUIStyle styles) {
        var settings = new TextGenerationSettings {
            font = styles.font ? styles.font : GUI.skin.font,
            fontSize = styles.fontSize,
            color = styles.normal.textColor,
            fontStyle = styles.fontStyle,
            lineSpacing = styles.lineHeight,
            textAnchor = styles.alignment,
            resizeTextForBestFit = styles.wordWrap,
            horizontalOverflow = styles.clipping == TextClipping.Overflow ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap,
            verticalOverflow = styles.clipping == TextClipping.Overflow ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate,
            updateBounds = false,
            pivot = new Vector2(0.5f, 0.5f),
            scaleFactor = 1f,
            alignByGeometry = false,
        };
        return settings;
    }
}
