using UnityEngine;

public class ColorUtil {
    
    public static Color GetColor(string colorCode, float alpha = 1) {
        if (ColorUtility.TryParseHtmlString(colorCode, out Color color) == false) {
            return Color.white;
        }

        color.a = alpha;
        return color;
    }
}