using UnityEngine;

public static class ScriptableObjectUtil {

    public static bool TryCreateInstance<T>(out T instance) where T : ScriptableObject => (instance = ScriptableObject.CreateInstance<T>()) != null;
}
