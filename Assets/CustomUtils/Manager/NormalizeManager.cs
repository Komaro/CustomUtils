using UnityEngine;

public class NormalizeManager : Singleton<NormalizeManager> {

    public void SetNormalizeToLocalPosition(GameObject root, GameObject target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root == null) {
            Logger.TraceError($"{nameof(root)} is Null");
            return;
        }
        
        if (target != null) {
            SetNormalizeToLocalPosition(root, target.transform, xNormal, yNormal, xRange, yRange);
        } else {
            Logger.TraceError($"{nameof(target)} is Null || {nameof(RectTransform)}");
        }
    }

    public void SetNormalizeToLocalPosition(GameObject root, Transform target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root == null) {
            Logger.TraceError($"{nameof(root)} is Null");
            return;
        }
        
        if (root.transform is RectTransform rect) {
            SetNormalizeToLocalPosition(rect, target, xNormal, yNormal, xRange, yRange);
        } else {
            Logger.TraceError($"Missing Component || {nameof(RectTransform)}");
        }
    }
    
    public void SetNormalizeToLocalPosition(RectTransform root, Transform target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root == null) {
            Logger.TraceError($"{nameof(root)} is Null");
            return;
        }
        
        if (target == null) {
            Logger.TraceError($"{nameof(target)} is Null");
            return;
        }

        target.localPosition = NormalizeToLocalPosition(root, xNormal, yNormal, xRange, yRange);
    }

    public Vector2 NormalizeToLocalPosition(GameObject root, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root == null) {
            Logger.TraceError($"{nameof(root)} is Null");
            return Vector2.zero;
        }
        
        if (root.transform is RectTransform rect) {
            return NormalizeToLocalPosition(rect, xNormal, yNormal, xRange, yRange);
        }

        Logger.TraceError($"Missing Component || {nameof(RectTransform)}");
        return Vector2.zero;
    }

    public Vector2 NormalizeToLocalPosition(RectTransform root, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root == null) {
            Logger.TraceError($"{nameof(root)} is Null");
            return Vector2.zero;
        }
        
        var collectionVector = -root.GetCollectionSizeVector();
        var xPosition = Mathf.Lerp(0f, root.rect.size.x, Mathf.Max(0, xNormal / xRange)) + collectionVector.x;
        var yPosition = Mathf.Lerp(0f, root.rect.size.y, Mathf.Max(0, yNormal / yRange)) + collectionVector.y;
        return new Vector2(xPosition, yPosition);
    }
}
