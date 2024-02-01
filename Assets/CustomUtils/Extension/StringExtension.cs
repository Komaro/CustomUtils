using System;
using System.Globalization;
using System.Text.RegularExpressions;

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
    public static string GetTitleCase(this string strText) => _textInfo?.ToTitleCase(strText);
    public static string GetForceTitleCase(this string strText) => GetTitleCase(strText.ToLower());
    public static string GetTitleCase<T>(this T type) where T : Enum => _textInfo?.ToTitleCase(type.ToString());
    public static bool EqualsFast(this string content, string comparedString) => content.Equals(comparedString, StringComparison.Ordinal);
}