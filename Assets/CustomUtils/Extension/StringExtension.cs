using System;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class StringExtension {

    private static readonly StringBuilder _builder = new();
    private static readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;

    public static bool TryGetBetween(this string content, string startMatch, string endMatch, out string betweenText, StringComparison comp = StringComparison.Ordinal) {
        betweenText = content.GetBetween(startMatch, endMatch, comp);
        return string.IsNullOrEmpty(betweenText) == false;
    }

    public static string GetBetween(this string content, string startMatch, string endMatch, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(startMatch) && content.Contains(endMatch)) {
            var startIndex = content.IndexOf(startMatch, comp) + startMatch.Length;
            var endIndex = content.IndexOf(endMatch, startIndex, comp);
            return content.Substring(startIndex, endIndex - startIndex);
        }

        return string.Empty;
    }

    public static string GetAfter(this string content, string matchString, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(matchString)) {
            var startIndex = content.LastIndexOf(matchString, comp) + matchString.Length;
            return content.Length <= startIndex ? string.Empty : content.Substring(startIndex);
        }

        return string.Empty;
    }

    public static string GetAfterFirst(this string content, string matchString, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(matchString)) {
            var startIndex = content.IndexOf(matchString, comp) + matchString.Length;
            return content.Length <= startIndex ? string.Empty : content.Substring(startIndex);
        }

        return string.Empty;
    }

    public static string GetBefore(this string content, string matchString, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(matchString)) {
            var endIndex = content.IndexOf(matchString, comp);
            return content.Substring(0, endIndex);
        }

        return string.Empty;
    }

    public static bool WrappedIn(this string content, string match, StringComparison comp = StringComparison.Ordinal) => content.StartsWith(match, comp) && content.EndsWith(match, comp);
    public static string GetTitleCase(this string content) => _textInfo?.ToTitleCase(content);
    public static string GetForceTitleCase(this string content) => GetTitleCase(content.ToLower());
    public static bool EqualsFast(this string content, string comparedString) => content.Equals(comparedString, StringComparison.Ordinal);
    
    public static byte[] GetBytes(this string content, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetBytes(content),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetBytes(content),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetBytes(content),
        _ => Encoding.UTF8.GetBytes(content)
    };

    public static byte[] GetRawBytes(this string content) => Convert.FromBase64String(content);

    public static string GetColorString(this string content, Color color) {
        if (string.IsNullOrEmpty(content) == false) {
            return $"<color=#{color.GetColorCode()}>{content}</color>";
        }
        
        return content;
    }
}

public enum ENCODING_FORMAT {
    UTF_8,
    UTF_32,
    UNICODE,
    ASCII,
}