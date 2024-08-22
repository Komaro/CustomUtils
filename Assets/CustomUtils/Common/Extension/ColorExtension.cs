using UnityEngine;
using SystemColor = System.Drawing.Color;

public static class ColorExtension {
    
    public static Color SetAlpha(ref this Color color, float alpha) => new(color.r, color.g, color.b, alpha);
    
    public static string ToHex(ref this Color color, bool hasAlpha = false) => $"#{(hasAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color))}";
    public static string ToHex(ref this SystemColor color, bool hasAlpha = false) => hasAlpha ? $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
