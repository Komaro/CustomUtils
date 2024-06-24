using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

public class StringUtil {

    private static readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;
    private static readonly List<char> _listSpecialCharacters = new() { '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '_', '+', '|', '<', '>', '?', '/', '{', '}', ' ', '.', ',', ';', ':', '。', '、' };
    private static readonly byte[] arr;

    public static bool IsOverSizeANSI(string text, int maxSize) {
        var length = GetTextLength(text);
        return length > maxSize;
    }

    public static (int length, bool isOverSize) GetOverSizeANSICount(string text, int maxSize) {
        var length = GetTextLength(text);
        return (maxSize - length, length > maxSize);
    }

    public static int GetANSILeftSize(string text, int maxSize) {
        var textLength = GetTextLength(text);
        return maxSize - textLength;
    }

    private static int GetTextLength(string text) {
        var charArray = text.ToCharArray();
        var length = 0;

        foreach (var x in charArray) {
            length += IsEnglish(x) || IsNumeric(x) ? 1 : 2;
        }
        return length;
    }

    public static string GetStringToEncodingType(string strMsg, Encoding toType) => toType.GetString(Encoding.Convert(Encoding.Default, toType, Encoding.Default.GetBytes(strMsg)));

    public static bool IsEnglish(char ch) => 0x61 <= ch && ch <= 0x7A ||
                                             0x41 <= ch && ch <= 0x5A;

    public static bool IsKorean(char ch, bool isAllowInitial) {
        if (0xAC00 <= ch && ch <= 0xD7A3) {
            return true;
        }

        if (isAllowInitial) {
            if (0x1100 <= ch && ch <= 0x11FF || 
                0x3131 <= ch && ch <= 0x318E || 
                0xAC00 <= ch && ch <= 0xD7A3) {
                return true;
            }
        }
        
        return false;
    }

    public static bool IsJapanese(char ch) => 0x3040 <= ch && ch <= 0x309F || 
                                              0x30A0 <= ch && ch <= 0x30FF || 
                                              0x4E00 <= ch && ch <= 0x9FBF || 
                                              0x3005 == ch;

    public static bool IsChinese(char ch) => ch >= 0x4E00 && ch <= 0x9FFF || 
                                             ch >= 0x3400 && ch <= 0x4DB5 || 		
                                             ch >= 0x2E80 && ch <= 0x2EFF || 
                                             ch >= 0xF900 && ch <= 0xFA6A;

    public static bool IsThai(string ch) {
        var texts = ch.ToCharArray();

        if (texts.Length <= 0) {
            return false;
        }

        foreach (var t in texts) {
            if (t >= 0x0E01 && t <= 0x0E5B) {
                return true;
            }
        }
        
        return false;
    }

    public static bool IsLatin(char ch) => 0x0080 <= ch && ch <= 0x00FF || 
                                           0x1E00 <= ch && ch <= 0x1EFF || 
                                           0x0100 <= ch && ch <= 0x017F || 
                                           0x0180 <= ch && ch <= 0x024F || 
                                           0x2C60 <= ch && ch <= 0x2C7F || 
                                           0xA720 <= ch && ch <= 0xA7FF || 
                                           0xAB30 <= ch && ch <= 0xAB6F;

    public static bool IsCyrillic(char ch) => 0x0400 <= ch && ch <= 0x04FF || 
                                              0x2DE0 <= ch && ch <= 0x2DFF || 
                                              0xA640 <= ch && ch <= 0xA69F || 
                                              0x1C80 <= ch && ch <= 0x1C8F || 
                                              0x0500 <= ch && ch <= 0x052F;

    public static bool IsThai(char ch) => 0x0E00 <= ch && ch <= 0x0E7F;

    public static bool IsNumeric(char ch) {
        if (0x30 <= ch && ch <= 0x39) {
            Logger.TraceLog($"IsNumeric char = {ch}");
            return true;
        }
        
        return false;
    }

    public static bool IsBasicLatin(char ch) {
        if (0x0020 <= ch && ch <= 0x007F) {
            Logger.TraceLog($"IsBasicLatin char = {ch}");
            return true;
        }
        
        return false;
    }

    public static bool IsDoubleSpaceChar(string strText) {
        var charArray = strText.ToCharArray();
        bool bIsSpace = false;

        foreach (var ch in charArray) {
            if (ch.Equals(' ')) {
                if (bIsSpace) {
                    return true;
                }

                bIsSpace = true;
                continue;
            }

            bIsSpace = false;
        }

        return bIsSpace;
    }

    public static bool IsStartSpaceChar(string strText) => strText?.ToCharArray()[0] == ' ';
    public static bool IsIncludedSpaceChar(string strText) => strText.Contains(" ");
    public static bool IsIncludedLineFeed(string strText) => strText.Contains("\n");
    public static bool IsAvailableSpecialCharacters(char ch) => _listSpecialCharacters.Contains(ch);
    public static string GetTitleCase(string strText) => _textInfo?.ToTitleCase(strText);
    public static string GetForceTitleCase(string strText) => GetTitleCase(strText.ToLower(Constants.Culture.DEFAULT_CULTURE_INFO));
    public static string GetTitleCase<T>(T type) where T : Enum => _textInfo?.ToTitleCase(type.ToString());
}