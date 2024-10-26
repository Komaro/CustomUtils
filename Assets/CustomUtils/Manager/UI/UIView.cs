using System;
using UnityEngine;

// todo. service 처리용
public interface IUIView {

    public void ChangeViewModel(UIViewModel viewModel);
}

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
        model?.Dispose();
        model = (TViewModel) viewModel;
        AttachModelChangedCallback();
    }

    protected abstract void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args);
}