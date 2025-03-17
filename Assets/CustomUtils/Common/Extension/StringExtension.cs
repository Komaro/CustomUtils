using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class StringExtension {

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

    public static string GetAfter(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(matchContent)) {
            var startIndex = content.LastIndexOf(matchContent, comp) + (includeMatch ? 0 : matchContent.Length);
            return content.Length <= startIndex ? string.Empty : content.Substring(startIndex);
        }

        return string.Empty;
    }

    public static string GetAfterFirst(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(matchContent)) {
            var startIndex = content.IndexOf(matchContent, comp) + (includeMatch ? 0 : matchContent.Length);
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

    public static string GetUpperBeforeSpace(this string content) => Constants.Regex.UPPER_UNICODE_REGEX.Replace(content, "$1").Trim();

    public static IEnumerable<string> GetCaseVariation(this string content) {
        yield return content;
        yield return content.ToUpper();
        yield return content.ToLower();
    }
    
    public static IEnumerable<string> GetPossibleVariantCases(this string content) {
        yield return content;
        yield return content.ToNoSpace();
        yield return content.Replace(" ", "_");
    }
    
    public static IEnumerable<string> GetAllPossibleVariantCases(this string content) => GetPossibleVariantCases(content).SelectMany(GetCaseVariation);

    public static string GetForceTitleCase(this string content) => GetTitleCase(content.ToLower());
    public static string GetTitleCase(this string content) => _textInfo?.ToTitleCase(content);
    
    public static int GetByteCount(this string content, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetByteCount(content),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetByteCount(content),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetByteCount(content),
        _ => Encoding.UTF8.GetByteCount(content)
    };

    public static byte[] ToBytes(this string content, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetBytes(content),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetBytes(content),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetBytes(content),
        _ => Encoding.UTF8.GetBytes(content)
    };

    public static byte[] ToRawBytes(this string content) => Convert.FromBase64String(content);

    public static string GetColorString(this string content, Color color) => string.IsNullOrEmpty(content) == false ? $"<color=#{color.ToHex()}>{content}</color>" : content;

    public static bool WrappedIn(this string content, string matchContent, StringComparison comparison = StringComparison.Ordinal) => content.StartsWith(matchContent, comparison) && content.EndsWith(matchContent, comparison);
    public static bool EqualsFast(this string content, string comparedString) => content.Equals(comparedString, StringComparison.Ordinal);
    public static bool ContainsFast(this string content, string containedContent) => content.Contains(containedContent, StringComparison.Ordinal);

    public static string ToNoSpace(this string content) => content.Replace(" ", string.Empty);

    public static string GetFileNameFast(this string content) => content.Split('.')?[0];

    public static string AutoSwitchExtension(this string content,  string extension) {
        if (content.ContainsExtension(extension) == false) {
            return Path.ChangeExtension(content, extension.FixExtension());
        }

        return content;
    }

    public static string FixExtension(this string content) => content.IsExtension() == false ? content.Insert(0, ".") : content;
    public static bool ContainsExtension(this string content, string extension) => Path.HasExtension(content) && Path.GetExtension(content).EqualsFast(extension);
    public static bool IsExtension(this string content) => content.StartsWith('.');

    #region [ToString]

    public static string ToStringSpan<T>(this Span<T> span, char separator = ' ') => span.ToArray().ToStringCollection(separator);
    public static string ToStringSpan<T>(this Span<T> span, string separator) => span.ToArray().ToStringCollection(separator);
    public static string ToStringSpan<T>(this Span<T> span, Func<T, string> selector, char separator = ' ') => span.ToArray().ToStringCollection(selector, separator);
    public static string ToStringSpan<T>(this Span<T> span, Func<T, string> selector, string separator) => span.ToArray().ToStringCollection(selector, separator);
    
    public static string ToStringSpan<T>(this ReadOnlySpan<T> span, char separator = ' ') => span.ToArray().ToStringCollection(separator);
    public static string ToStringSpan<T>(this ReadOnlySpan<T> span, string separator) => span.ToArray().ToStringCollection(separator);
    public static string ToStringSpan<T>(this ReadOnlySpan<T> span, Func<T, string> selector, char separator = ' ') => span.ToArray().ToStringCollection(selector, separator);
    public static string ToStringSpan<T>(this ReadOnlySpan<T> span, Func<T, string> selector, string separator) => span.ToArray().ToStringCollection(selector, separator);

    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, char separator = ' ') => string.Join(separator, enumerable);
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, string separator) => string.Join(separator, enumerable);
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, Func<T, string> selector, char separator = ' ') => string.Join(separator, enumerable.Select(selector.Invoke));
    public static string ToStringCollection<T>(this IEnumerable<T> enumerable, Func<T, string> selector, string separator) => string.Join(separator, enumerable.Select(selector.Invoke));

    #endregion
}

public enum ENCODING_FORMAT {
    UTF_8,
    UTF_32,
    UNICODE,
    ASCII,
}