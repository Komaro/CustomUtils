using UnityEngine;

public class NormalizeManager : Singleton<NormalizeManager> {

    public void SetNormalizeToLocalPosition(GameObject root, GameObject target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (target != null) {
            SetNormalizeToLocalPosition(root, target.transform, xNormal, yNormal, xRange, yRange);
        } else {
            Logger.TraceError($"{nameof(target)} is Null || {nameof(RectTransform)}");
        }
    }

    public void SetNormalizeToLocalPosition(GameObject root, Transform target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root?.transform is RectTransform rect) {
            SetNormalizeToLocalPosition(rect, target, xNormal, yNormal, xRange, yRange);
        } else {
            Logger.TraceError($"Missing Component || {nameof(RectTransform)}");
        }
    }
    
    public void SetNormalizeToLocalPosition(RectTransform root, Transform target, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (target == null) {
            Logger.TraceError($"{nameof(target)} is Null");
            return;
        }

        target.localPosition = NormalizeToLocalPosition(root, xNormal, yNormal, xRange, yRange);
    }

    public Vector2 NormalizeToLocalPosition(GameObject root, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        if (root?.transform is RectTransform rect) {
            return NormalizeToLocalPosition(rect, xNormal, yNormal, xRange, yRange);
        }

        Logger.TraceError($"Missing Component || {nameof(RectTransform)}");
        return Vector2.zero;
    }

    public Vector2 NormalizeToLocalPosition(RectTransform root, float xNormal, float yNormal, float xRange = 1f, float yRange = 1f) {
        var collectionVector = -root.GetCollectionSizeVector();
        var xPosition = Mathf.Lerp(0f, root.rect.size.x, Mathf.Max(0, xNormal / xRange)) + collectionVector.x;
        var yPosition = Mathf.Lerp(0f, root.rect.size.y, Mathf.Max(0, yNormal / yRange)) + collectionVector.y;
        return new Vector3(xPosition, yPosition);
    }
    
    public void TestCalculatePosition(Transform root, Transform target, float x, float y, float minRange = 0f, float maxRange = 1f) {
#if UNITY_EDITOR
        if (root.TryGetComponent<RectTransform>(out var rect)) {
            Logger.TraceError(rect.rect.size);
            Logger.TraceError($"Pivot || x = {rect.pivot.x} => {-(rect.pivot.x * rect.rect.size.x)} || y => {rect.pivot.y} => {-(rect.pivot.y * rect.rect.size.y)}");
            
            Logger.TraceError($"{target.position} || {target.position.normalized}");

            var calcX = Mathf.Max(minRange, x / maxRange);
            var xPosition = Mathf.Lerp(0f, rect.rect.size.x, calcX);
            var xCorrection = -(rect.pivot.x * rect.rect.size.x);
            Logger.TraceError($"x ({x}) => {calcX} => {xPosition} - {xCorrection} => {xPosition + xCorrection}");

            var calcY = Mathf.Max(minRange, y / maxRange);
            var yPosition = Mathf.Lerp(0f, rect.rect.size.y, calcY);
            var yCorrection = -(rect.pivot.y * rect.rect.size.y);
            Logger.TraceError($"y ({y}) => {calcY} => {yPosition} - {yCorrection} => {yPosition + yCorrection}");

            target.localPosition = new Vector2(xPosition + xCorrection, yPosition + yCorrection);
        }
#endif
    }
}
