using UnityEngine;

public abstract class UIController<TModel, TView> : MonoBehaviour where TView : UIView<TModel> where  TModel : new() {

    protected TModel model;
    protected TView view;

    public bool Initialized { get; protected set; }

    private void Awake() {
        view = GetComponent<TView>();
        OnAwake();
    }

    protected abstract void OnAwake();
    
    protected abstract void UpdateUI();

    public virtual void ChangeModel(TModel model) {
        this.model = model;
        UpdateUI();
    }
}