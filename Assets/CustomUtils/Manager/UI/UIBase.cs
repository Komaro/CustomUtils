using System;
using UnityEngine;

public interface IUIBase {

    void Init();
    void Clear();
    void SetData(object infoObject);
}

public abstract class UIBase : MonoBehaviour, IUIBase {
    
    public UIBase() {
        if (GetType().GetCustomAttributes(typeof(UIAttribute), false).Length <= 0) {
            throw new NotImplementedException($"{GetType()} Not Implemented CustomAttribute. Please Add {nameof(UIAttribute)}.");
        }
    }

    public abstract void Init();
    public abstract void Clear();
    public abstract void SetData(object infoObject);
    public virtual void Open() => SetActive(true);
    public virtual void Close() => SetActive(OnBack());
    public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);
    public virtual bool IsActive() => gameObject.activeInHierarchy;
    public virtual bool OnBack() => false;
    protected virtual void SetAnchor(GameObject go, int left, int bottom, int right, int top) {
        // TODO. Not Implemented
    }
}
