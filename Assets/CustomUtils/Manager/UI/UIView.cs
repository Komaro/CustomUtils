using UnityEngine;

public abstract class UIView<TModel> : MonoBehaviour where TModel : new() {

    protected TModel model = new();

    public virtual void ChangeModel(TModel model) => this.model = model;
}
