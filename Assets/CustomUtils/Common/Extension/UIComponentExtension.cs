using System;
using UnityEngine;

// TODO. 전체적인 구현 검토 및 최적화 필요
public static class UIComponentExtension {

    public static bool TryGetOrAddComponent<T>(this GameObject go, Type type, out T component) where T : Component => component = go.GetOrAddComponent<T>(type);
    
    public static T GetOrAddComponent<T>(this GameObject go, Type type) where T : Component => go.TryGetComponent(type, out var component) && component is T typedComponent ? typedComponent : go.AddComponent(type) as T;
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component => go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();
    
    public static bool TryFindGameObject(this Transform transform, string objectName, out GameObject findGameObject) => findGameObject = transform.FindGameObject(objectName);
    public static bool TryFindGameObject(this GameObject go, string objectName, out GameObject findGameObject) => findGameObject = go.FindGameObject(objectName);
    
    public static GameObject FindGameObject(this GameObject go, string objectName) => go.transform.FindTransform(objectName)?.gameObject;
    public static GameObject FindGameObject(this Transform transform, string objectName) => transform.FindTransform(objectName)?.gameObject;

    public static bool TryFindTransform(this GameObject go, string objectName, out Transform findTransform) => findTransform = go.FindTransform(objectName);
    public static bool TryFindTransform(this Transform transform, string objectName, out Transform findTransform) => findTransform = transform.Find(objectName);
    
    public static Transform FindTransform(this GameObject go, string objectName) => go.transform.FindTransform(objectName);
    
    public static Transform FindTransform(this Transform transform, string objectName) {
        if (transform.name == objectName) {
            return transform;
        }

        foreach (Transform tr in transform) {
            var find = tr.FindTransform(objectName);
            if (find) {
                return find;
            }
        }

        return null;
    }
    
    public static bool TryFindComponent<T>(this GameObject go, string objectName, out T component) where T : Component => go.transform.TryFindComponent(objectName, out component);
    public static bool TryFindComponent<T>(this Transform transform, string objectName, out T component) where T : Component => component = transform.FindComponent<T>(objectName);
    
    public static T FindComponent<T>(this GameObject go, string objectName) where T : Component => go.transform.FindComponent<T>(objectName);
    
    public static T FindComponent<T>(this Transform transform, string objectName) where T : Component {
        var findObject = transform.FindGameObject(objectName);
        if (!findObject) {
            Logger.TraceError($"{nameof(findObject)} is Null || {nameof(objectName)} = {objectName}");
            return null;
        }

        var component = findObject.GetComponent<T>();
        if (!component) {
            Logger.TraceError($"{nameof(component)} is null || {nameof(T)} = {typeof(T).FullName}");
            return null;
        }

        return component;
    }
}