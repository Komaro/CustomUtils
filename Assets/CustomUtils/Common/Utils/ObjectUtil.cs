using UnityEngine;

public static class ObjectUtil {
    
    public static bool TryInstantiate<T>(T obj, GameObject parent, out T instant) where T : Object => TryInstantiate(obj, parent.transform, out instant);
    public static bool TryInstantiate<T>(T obj, Transform parent, out T instant) where T : Object => (instant = Object.Instantiate(obj, parent)) != null;
    public static bool TryInstantiate<T>(T obj, out T instant) where T : Object => (instant = Object.Instantiate(obj)) != null;


    public static bool TryInstantiate(Object obj, GameObject parent, out Object instant) => TryInstantiate(obj, parent.transform, out instant);
    public static bool TryInstantiate(Object obj, Transform parent, out Object instant) => (instant = Object.Instantiate(obj, parent)) != null;
    public static bool TryInstantiate(Object obj, out Object instant) => (instant = Object.Instantiate(obj)) != null;
}
