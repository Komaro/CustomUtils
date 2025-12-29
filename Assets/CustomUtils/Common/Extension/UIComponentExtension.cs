using System;
using UnityEngine;

public static class UIComponentExtension {

    public static bool TryGetOrAddComponent<T>(this GameObject go, Type type, out T component) where T : class => (component = go.GetOrAddComponent<T>(type)) != null;

    public static T GetOrAddComponent<T>(this GameObject go, Type type) where T : class {
        if (go.TryGetComponent(type, out var component)) {
            return component as T;
        }
        
        return go.AddComponent(type) as T;
    }
    
    public static TComponent GetOrAddComponent<TComponent>(this GameObject go) where TComponent : Component => go.TryGetComponent<TComponent>(out var component) ? component : go.AddComponent<TComponent>();
    
    public static GameObject FindGameObject(this GameObject go, string objectName) => go.transform.FindTransform(objectName)?.gameObject;
    public static GameObject FindGameObject(this Transform transform, string objectName) => transform.FindTransform(objectName)?.gameObject;

    public static Transform FindTransform(this GameObject go, string objectName) => go.transform.FindTransform(objectName);
    public static Transform FindTransform(this Transform transform, string objectName) {
        if (transform.name == objectName) {
            return transform;
        }

        foreach (Transform tr in transform.transform) {
            if (tr.TryFindTransform(objectName, out var findTransform)) {
                return findTransform;
            }
        }

        return null;
    }

    public static bool TryFindTransform(this GameObject go, string objectName, out Transform findTransform) {
        findTransform = go.FindTransform(objectName);
        return findTransform != null;
    }
	
    public static bool TryFindTransform(this Transform transform, string objectName, out Transform findTransform) {
        findTransform = transform.Find(objectName);
        return findTransform != null;
    }
	
    public static bool TryFindGameObject(this Transform transform, string objectName, out GameObject findGameObject) {
        findGameObject = transform.FindGameObject(objectName);
        return findGameObject != null;
    }
	
    public static bool TryFindGameObject(this GameObject go, string objectName, out GameObject findGameObject) {
        findGameObject = go.FindGameObject(objectName);
        return findGameObject != null;
    }
	
    public static TComponent FindComponent<TComponent>(this GameObject go, string objectName) => go.transform.FindComponent<TComponent>(objectName);

    public static TComponent FindComponent<TComponent>(this Transform transform, string objectName) {
        var findObject = transform.FindGameObject(objectName);
        if (findObject == null) {
            Logger.TraceError($"{nameof(findObject)} is Null || {nameof(objectName)} = {objectName}");
            return default;
        }

        var findComponent = findObject.GetComponent<TComponent>();
        if (findComponent == null) {
            Logger.TraceError($"{nameof(findComponent)} is Null || T = {typeof(TComponent).FullName}");
            return default;
        }

        return findComponent;
    }

    public static bool TryFindComponent<TComponent>(this GameObject go, string objectName, out TComponent component) => go.transform.TryFindComponent(objectName, out component);
	
    public static bool TryFindComponent<TComponent>(this Transform transform, string objectName, out TComponent component) {
        component = transform.FindComponent<TComponent>(objectName);
        return component != null;
    }
}