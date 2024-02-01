using System;
using UnityEngine;

public interface IGameObject {
    
    GameObject gameObject { get; }
    Transform transform { get; }
}

public class ScriptObject : IGameObject {
    
    public GameObject gameObject { get; private set; }
    public Transform transform { get; private set; }
    public bool activeInHierarchy => gameObject != null && gameObject.activeInHierarchy;

    public ScriptObject(GameObject root) {
        if (root == null)
            throw new NullReferenceException("GameObject is null.");

        gameObject = root;
        transform = gameObject.transform;
    }

    public void SetActive(bool value) => gameObject?.SetActive(value);
}