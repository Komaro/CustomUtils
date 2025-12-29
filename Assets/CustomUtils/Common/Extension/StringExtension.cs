using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class StringExtension {

    private static readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;

    // TODO. Need Modernize
    public static bool TryGetBetween(this string content, string startMatch, string endMatch, out string betweenText, StringComparison comp = StringComparison.Ordinal) {
        betweenText = content.GetBetween(startMatch, endMatch, comp);
        return string.IsNullOrEmpty(betweenText) == false;
    }

    // TODO. Need Modernize
    public static string GetBetween(this string content, string startMatch, string endMatch, StringComparison comp = StringComparison.Ordinal) {
        if (content.Contains(startMatch) && content.Contains(endMatch)) {
            var startIndex = content.IndexOf(startMatch, comp) + startMatch.Length;
            var endIndex = content.IndexOf(endMatch, startIndex, comp);
            return content.Substring(startIndex, endIndex - startIndex);
        }

        return string.Empty;
    }

    public static bool TryGetBetweenSpan(this string content, out ReadOnlySpan<char> span, string startMatch, string endMatch, StringComparison comp = StringComparison.Ordinal) => (span = content.GetBetweenSpan(startMatch, endMatch, comp)) != ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetBetweenSpan(this string content, string startMatch, string endMatch, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOfFirst(out var startIndex, startMatch, false, comp) && content.TryIndexOfFirst(out var endIndex, endMatch, false, comp) ? content.AsSpan(startIndex, endIndex - startIndex) : ReadOnlySpan<char>.Empty;

    public static ReadOnlySpan<char> GetAfterSpan(this ReadOnlySpan<char> content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content[index..] : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetAfterSpanFirst(this ReadOnlySpan<char> content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content[index..] : ReadOnlySpan<char>.Empty;

    public static ReadOnlySpan<char> GetAfterSpan(this ReadOnlySpan<char> content, string matchContent, bool includeMatch = false) => content.TryIndexOf(out var index, matchContent, includeMatch) ? content[index..] : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetAfterSpanFirst(this ReadOnlySpan<char> content, string matchContent, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchContent, includeMatch) ? content[index..] : ReadOnlySpan<char>.Empty;
    
    public static ReadOnlySpan<char> GetBeforeSpan(this ReadOnlySpan<char> content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content[..index] : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetBeforeSpanFirst(this ReadOnlySpan<char> content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content[..index] : ReadOnlySpan<char>.Empty;
    
    public static ReadOnlySpan<char> GetBeforeSpan(this ReadOnlySpan<char> content, string matchContent, bool includeMatch = false) => content.TryIndexOf(out var index, matchContent, includeMatch) ? content[..index] : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetBeforeSpanFirst(this ReadOnlySpan<char> content, string matchContent, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchContent, includeMatch) ? content[..index] : ReadOnlySpan<char>.Empty;
    
    
    public static ReadOnlySpan<char> GetAfterSpan(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content.AsSpan(index) : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetAfterSpanFirst(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content.AsSpan(index) : ReadOnlySpan<char>.Empty;

    public static ReadOnlySpan<char> GetAfterSpan(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOf(out var index, matchContent, includeMatch, comp) ? content.AsSpan(index) : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetAfterSpanFirst(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOfFirst(out var index, matchContent, includeMatch, comp) ? content.AsSpan(index) : ReadOnlySpan<char>.Empty;
    
    public static ReadOnlySpan<char> GetBeforeSpan(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content.AsSpan(0, index) : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetBeforeSpanFirst(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content.AsSpan(0, index) : ReadOnlySpan<char>.Empty;
    
    public static ReadOnlySpan<char> GetBeforeSpan(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOf(out var index, matchContent, includeMatch) ? content.AsSpan(0, index) : ReadOnlySpan<char>.Empty;
    public static ReadOnlySpan<char> GetBeforeSpanFirst(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOfFirst(out var index, matchContent, includeMatch) ? content.AsSpan(0, index) : ReadOnlySpan<char>.Empty;

    
    public static string GetAfter(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content[index..] : string.Empty;
    public static string GetAfterFirst(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content[index..] : string.Empty;

    public static string GetAfter(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOf(out var index, matchContent, includeMatch, comp) ? content[index..] : string.Empty;
    public static string GetAfterFirst(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOfFirst(out var index, matchContent, includeMatch, comp) ? content[index..] : string.Empty;

    public static string GetBefore(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOf(out var index, matchChar, includeMatch) ? content[..index] : string.Empty;
    public static string GetBeforeFirst(this string content, char matchChar, bool includeMatch = false) => content.TryIndexOfFirst(out var index, matchChar, includeMatch) ? content[..index] : string.Empty;

    public static string GetBefore(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOf(out var index, matchContent, includeMatch, comp) ? content[..index] : string.Empty;
    public static string GetBeforeFirst(this string content, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => content.TryIndexOfFirst(out var index, matchContent, includeMatch, comp) ? content[..index] : string.Empty;

    public static string GetUpperBeforeSpace(this string content) => Constants.Regex.UPPER_UNICODE_REGEX.Replace(content, "1$1").Trim();

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
    public static bool WrappedIn(this ReadOnlySpan<char> content, string matchContent, StringComparison comparison = StringComparison.Ordinal) => content.StartsWith(matchContent, comparison) && content.EndsWith(matchContent, comparison);
    
    public static bool EqualsFast(this string content, string comparedString) => content.Equals(comparedString, StringComparison.Ordinal);
    public static bool EqualsFast(this ReadOnlySpan<char> content, string comparedString) => content.Equals(comparedString, StringComparison.Ordinal);
    
    public static bool ContainsFast(this string content, string containedContent) => content.Contains(containedContent, StringComparison.Ordinal);
    public static bool ContainsFast(this ReadOnlySpan<char> content, string containedContent) => content.Contains(containedContent, StringComparison.Ordinal);


    public static string ToNoSpace(this string content) => content.Replace(" ", string.Empty);

    public static string GetFileNameFast(this string content) => content.Split('.')?[0];
    
    public static string AutoSwitchExtension(this string content, string extension) => content.ContainsExtension(extension) == false ? Path.ChangeExtension(content, extension.FixExtension()) : content;
    public static string FixExtension(this string content) => content.IsExtension() == false ? content.Insert(0, ".") : content;
    public static bool ContainsExtension(this string content, string extension) => Path.HasExtension(content) && Path.GetExtension(content).EqualsFast(extension);
    public static bool IsExtension(this string content) => content.StartsWith('.');
    
    public static string FixSeparator(this string content) => content.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static string FixLineBreak(this string content) => content.Replace("\r\n", "\n").Replace("\r", "\n");

    public static bool TryIndexOf(this string content, out int index, char matchChar, bool includeMatch = false) => (index = content.LastIndexOf(matchChar) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;
    public static bool TryIndexOfFirst(this string content, out int index, char matchChar, bool includeMatch = false) => (index = content.IndexOf(matchChar) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;
    
    public static bool TryIndexOf(this string content, out int index, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => (index = content.LastIndexOf(matchContent, comp) + (includeMatch ? 0 : matchContent.Length)) >= 0 && content.Length > index;
    public static bool TryIndexOfFirst(this string content, out int index, string matchContent, bool includeMatch = false, StringComparison comp = StringComparison.Ordinal) => (index = content.IndexOf(matchContent, comp) + (includeMatch ? 0 : matchContent.Length)) >= 0 && content.Length > index;

    public static bool TryIndexOf(this ReadOnlySpan<char> content, out int index, char matchChar, bool includeMatch = false) => (index = content.LastIndexOf(matchChar) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;
    public static bool TryIndexOfFirst(this ReadOnlySpan<char> content, out int index, char matchChar, bool includeMatch = false) => (index = content.IndexOf(matchChar) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;
    
    public static bool TryIndexOf(this ReadOnlySpan<char> content, out int index, string matchString, bool includeMatch = false) => (index = content.LastIndexOf(matchString) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;
    public static bool TryIndexOfFirst(this ReadOnlySpan<char> content, out int index, string matchString, bool includeMatch = false) => (index = content.IndexOf(matchString) + (includeMatch ? 0 : 1)) >= 0 && content.Length > index;

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