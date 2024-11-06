using System;
using UnityEngine;

public class UIViewAttribute : Attribute {
    
    public string prefab;

    public UIViewAttribute(string prefab) => this.prefab = prefab;
}

public interface IUIView {

    public void ChangeViewModel(UIViewModel viewModel);
    public GameObject gameObject { get; }
    public Transform transform { get; }

    public void SetActive(bool isActive) => gameObject.SetActive(isActive);
}

[RequiresAttributeImplementation(typeof(UIViewAttribute))]
public abstract class UIView<TViewModel> : MonoBehaviour, IUIView where TViewModel : UIViewModel {

    protected TViewModel model;
    
    protected virtual void OnEnable() {
        if (model != null) {
            AttachModelChangedCallback();
        }
    }

    protected virtual void AttachModelChangedCallback() {
        if (model.IsAlreadyOnChanged()) {
            model.Clear();
        }
        
        model.OnModelChanged += OnNotifyModelChanged;
    }

    public virtual void ChangeViewModel(UIViewModel viewModel) {
        if (viewModel is TViewModel castViewModel) {
            model?.Dispose();
            model = castViewModel;
            AttachModelChangedCallback();
        } else {
            throw new InvalidCastException($"{viewModel.GetType().Name} cannot be cast to {typeof(TViewModel).Name}");
        }
    }

    protected abstract void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args);
}