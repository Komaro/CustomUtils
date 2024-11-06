using UnityEngine;

public static class TransformExtension {

    public static int GetHierarchyDepth(this Transform transform) {
        var depth = 0;
        while (transform.parent != null) {
            depth++;
            transform = transform.parent;
        }

        return depth;
    }
    
    public static float GetCollectionScale(this RectTransform rect) {
        var collectionScale = rect.GetCollectionScaleVector();
        return Mathf.Min(collectionScale.x, collectionScale.y);
    }

    public static Vector2 GetCollectionSizeVector(this RectTransform rect) => new Vector2(rect.pivot.x * rect.rect.size.x, rect.pivot.y * rect.rect.size.y);
    public static Vector2 GetCollectionScaleVector(this RectTransform rect) => rect.rect.size / new Vector2(Screen.height, Screen.width);

    public static Vector3 GetCollectionLocalPosition(this RectTransform rect, PivotPresets collectionType) {
        switch (rect.GetAnchorType()) {
            case AnchorPresets.TOP_LEFT:
            case AnchorPresets.TOP_RIGHT:
            case AnchorPresets.BOTTOM_LEFT:
            case AnchorPresets.BOTTOM_RIGHT:
                return rect.localPosition;
            default:
                var collectionPivot = collectionType.GetPivot();
                if (collectionPivot == rect.pivot) {
                    return rect.localPosition;
                }

                return rect.localPosition + new Vector3(rect.sizeDelta.x * (collectionPivot.x - rect.pivot.x), rect.sizeDelta.y * (collectionPivot.y - rect.pivot.y), 0);
        }
    }
    
    public static void SetAnchor(this RectTransform rect, AnchorPresets presetType) {
        switch (presetType) {
            case AnchorPresets.TOP_LEFT:
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                break;
            case AnchorPresets.TOP_CENTER:
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                break;
            case AnchorPresets.TOP_RIGHT:
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                break;
            case AnchorPresets.MIDDLE_LEFT:
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                break;
            case AnchorPresets.MIDDLE_CENTER:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPresets.MIDDLE_RIGHT:
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                break;
            case AnchorPresets.BOTTOM_LEFT:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                break;
            case AnchorPresets.BOTTOM_CENTER:
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                break;
            case AnchorPresets.BOTTOM_RIGHT:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                break;
            case AnchorPresets.HOR_STRETCH_TOP:
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                break;
            case AnchorPresets.HOR_STRETCH_MIDDLE:
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                break;
            case AnchorPresets.HOR_STRETCH_BOTTOM:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                break;
            case AnchorPresets.VERT_STRETCH_LEFT:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 1);
                break;
            case AnchorPresets.VERT_STRETCH_CENTER:
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 1);
                break;
            case AnchorPresets.VERT_STRETCH_RIGHT:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 1);
                break;
            case AnchorPresets.STRETCH_ALL:
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 1);
                break;
        }
    }

    public static Vector2 GetPivot(this PivotPresets preset) {
        return preset switch {
            PivotPresets.TOP_LEFT => new Vector2(0, 1),
            PivotPresets.TOP_CENTER => new Vector2(0.5f, 1),
            PivotPresets.TOP_RIGHT => new Vector2(1, 1),
            PivotPresets.MIDDLE_LEFT => new Vector2(0, 0.5f),
            PivotPresets.MIDDLE_CENTER => new Vector2(0.5f, 0.5f),
            PivotPresets.MIDDLE_RIGHT => new Vector2(1, 0.5f),
            PivotPresets.BOTTOM_LEFT => new Vector2(0, 0),
            PivotPresets.BOTTOM_CENTER => new Vector2(0.5f, 0),
            PivotPresets.BOTTOM_RIGHT => new Vector2(1, 0),
            _ => new Vector2(0.5f, 0.5f)
        };
    }

    public static void SetPivot(this RectTransform rect, PivotPresets preset) {
        switch (preset) {
            case PivotPresets.TOP_LEFT:
                rect.pivot = new Vector2(0, 1);
                break;
            case PivotPresets.TOP_CENTER:
                rect.pivot = new Vector2(0.5f, 1);
                break;
            case PivotPresets.TOP_RIGHT:
                rect.pivot = new Vector2(1, 1);
                break;
            case PivotPresets.MIDDLE_LEFT:
                rect.pivot = new Vector2(0, 0.5f);
                break;
            case PivotPresets.MIDDLE_CENTER:
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
            case PivotPresets.MIDDLE_RIGHT:
                rect.pivot = new Vector2(1, 0.5f);
                break;
            case PivotPresets.BOTTOM_LEFT:
                rect.pivot = new Vector2(0, 0);
                break;
            case PivotPresets.BOTTOM_CENTER:
                rect.pivot = new Vector2(0.5f, 0);
                break;
            case PivotPresets.BOTTOM_RIGHT:
                rect.pivot = new Vector2(1, 0);
                break;
        }
    }

    public static AnchorPresets GetAnchorType(this RectTransform rect) {
        var minAnchor = rect.anchorMin;
        var maxAnchor = rect.anchorMax;
        return (minAnchor.x, minAnchor.y, maxAnchor.x, maxAnchor.y) switch {
            (0f, 1f, 0f, 1f) => AnchorPresets.TOP_LEFT,
            (0.5f, 1f, 0.5f, 1f) => AnchorPresets.TOP_CENTER,
            (1f, 1f, 1f, 1f) => AnchorPresets.TOP_RIGHT,

            (0f, 0.5f, 0f, 0.5f) => AnchorPresets.MIDDLE_LEFT,
            (0.5f, 0.5f, 0.5f, 0.5f) => AnchorPresets.MIDDLE_CENTER,
            (1f, 0.5f, 1f, 0.5f) => AnchorPresets.MIDDLE_RIGHT,

            (0f, 0f, 0f, 0f) => AnchorPresets.BOTTOM_LEFT,
            (0.5f, 0f, 0.5f, 0f) => AnchorPresets.BOTTOM_CENTER,
            (1f, 0f, 1f, 0f) => AnchorPresets.BOTTOM_RIGHT,

            (0f, 1f, 1f, 1f) => AnchorPresets.HOR_STRETCH_TOP,
            (0f, 0.5f, 1f, 0.5f) => AnchorPresets.HOR_STRETCH_BOTTOM,
            (0f, 0f, 1f, 0f) => AnchorPresets.HOR_STRETCH_BOTTOM,

            (0f, 0f, 0f, 1f) => AnchorPresets.VERT_STRETCH_LEFT,
            (0.5f, 0f, 0.5f, 1f) => AnchorPresets.VERT_STRETCH_CENTER,
            (1f, 0f, 1f, 1f) => AnchorPresets.VERT_STRETCH_RIGHT,

            (0f, 0f, 1f, 1f) => AnchorPresets.STRETCH_ALL,

            _ => default
        };
    }

    public static bool TryGetRectTransform(this Transform transform, out RectTransform rect) {
        rect = transform.GetRectTransform();
        return rect != null;
    }

    public static RectTransform GetRectTransform(this Transform transform) => transform is RectTransform rect ? rect : null;

    public static Vector2 GetAbsoluteSize(this RectTransform rect) => rect.rect.size;
    public static bool IsStretch(this RectTransform rect) => AnchorPresets.HOR_STRETCH_TOP <= rect.GetAnchorType();
    public static bool IsStretchAll(this RectTransform rect) => rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
    public static bool IsHorizontalStretch(this RectTransform rect) => rect.anchorMin.x == 0 && rect.anchorMax.x >= 1;
    public static bool IsVerticalStretch(this RectTransform rect) => rect.anchorMin.y == 0 && rect.anchorMax.y >= 1;
}

public enum AnchorPresets {
    NONE,

    TOP_LEFT,
    TOP_CENTER,
    TOP_RIGHT,

    MIDDLE_LEFT,
    MIDDLE_CENTER,
    MIDDLE_RIGHT,

    BOTTOM_LEFT,
    BOTTOM_CENTER,
    BOTTOM_RIGHT,

    VERT_STRETCH_LEFT,
    VERT_STRETCH_RIGHT,
    VERT_STRETCH_CENTER,

    HOR_STRETCH_TOP,
    HOR_STRETCH_MIDDLE,
    HOR_STRETCH_BOTTOM,

    STRETCH_ALL
}

public enum PivotPresets {
    TOP_LEFT,
    TOP_CENTER,
    TOP_RIGHT,

    MIDDLE_LEFT,
    MIDDLE_CENTER,
    MIDDLE_RIGHT,

    BOTTOM_LEFT,
    BOTTOM_CENTER,
    BOTTOM_RIGHT
}