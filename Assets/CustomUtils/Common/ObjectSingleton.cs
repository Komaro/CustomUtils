using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectSingleton<T> where T : Component {

    private static GameObject _root;

    private static T _instance;

    public static T instance {
        get {
            if (_instance == null) {
                var go = new GameObject(typeof(T).Name);
                go.transform.SetParent(_root.transform);
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<T>();
            }

            return _instance;
        }
    }
    
    public static T inst => instance;

    static ObjectSingleton() {
        _root = new GameObject("ObjectSingleton");
        Object.DontDestroyOnLoad(_root);
    }
}