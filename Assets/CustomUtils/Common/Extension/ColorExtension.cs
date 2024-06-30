using UnityEngine;

public static class ColorExtension {

    public static string GetColorCode(this Color color) => ColorUtility.ToHtmlStringRGB(color);
}
