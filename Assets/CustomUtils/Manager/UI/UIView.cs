using UnityEngine;

public abstract class UIView<TViewModel> : MonoBehaviour where TViewModel : new() {

    protected TViewModel model = new();

    public virtual void ChangeModel(TViewModel model) => this.model = model;
}
