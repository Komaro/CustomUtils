using System;
using UnityEngine;

public class UIViewAttribute : PriorityAttribute {
    
    public string prefab;

    public UIViewAttribute(string prefab) => this.prefab = prefab;
}

public interface IUIView {

    public GameObject gameObject { get; }
    public Transform transform { get; }

    public void AttachModelChangedCallback();
    public void ChangeViewModel(UIViewModel viewModel);
    public void SetActive(bool isActive) => gameObject.SetActive(isActive);
}

[RequiresAttributeImplementation(typeof(UIViewAttribute))]
public abstract class UIView<TViewModel> : MonoBehaviour, IUIView where TViewModel : UIViewModel, new() {

    [SerializeField] 
    protected bool ignoreAttachModelChangedCallback;

    protected TViewModel viewModel;

    protected virtual void Awake() {
        if (GetType().TryGetCustomAttribute<UIViewAttribute>(out var attribute)) {
            transform.SetSiblingIndex((int)attribute.priority);
        }
    }

    protected virtual void OnEnable() {
        if (viewModel == null) {
            Logger.TraceLog($"{nameof(viewModel)} is null. Create default {typeof(TViewModel).Name}", Color.yellow);
            viewModel = new TViewModel();
        }
        
        if (ignoreAttachModelChangedCallback == false) {
            AttachModelChangedCallback();
        }
    }

    protected virtual void OnDisable() {
        if (viewModel != null && ignoreAttachModelChangedCallback && viewModel.IsAlreadyOnChanged()) {
            viewModel.Clear();
        }
    }

    public virtual void AttachModelChangedCallback() {
        if (viewModel.IsAlreadyOnChanged()) {
            viewModel.Clear();
        }
        
        viewModel.OnModelChanged += OnNotifyModelChanged;
    }

    public virtual void ChangeViewModel(UIViewModel viewModel) {
        if (viewModel is TViewModel castViewModel) {
            this.viewModel?.Dispose();
            this.viewModel = castViewModel;
            if (ignoreAttachModelChangedCallback == false) {
                AttachModelChangedCallback();
            }
        } else {
            throw new InvalidCastException<TViewModel>(viewModel.GetType());
        }
    }

    protected abstract void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args);
}