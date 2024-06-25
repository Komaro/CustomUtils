using UnityEngine;

public static class RectExtension {

    public static Rect GetCenterRect(this Rect rect, float squareSize) => rect.GetCenterRect(squareSize, squareSize);
    public static Rect GetCenterRect(this Rect rect, Vector2 size) => rect.GetCenterRect(size.x, size.y);

    public static Rect GetCenterRect(this Rect rect, float width, float height) {
        if (width < rect.width) {
            var diff = rect.width - width;
            rect.xMin += diff * .5f;
            rect.xMax -= diff * .5f;
        }
            
        if (height < rect.height) {
            var diff = rect.height - height;
            rect.yMin += diff * .5f;
            rect.yMax -= diff * .5f;
        }

        return rect;
    }

    public static Vector2 GetCenterPosition(this Rect rect) {
        var position = rect.position;
        position.x += rect.size.x / 2;
        position.y += rect.size.y / 2;
        return position;
    }
}