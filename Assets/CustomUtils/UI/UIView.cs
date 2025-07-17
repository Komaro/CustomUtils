using UnityEngine;

public class UIViewAttribute : PriorityAttribute {
    
    public string prefab;
    public bool isGlobal;

    public UIViewAttribute(string prefab) => this.prefab = prefab;
    public UIViewAttribute(string prefab, bool isGlobal) : this(prefab) => this.isGlobal = isGlobal;
}

public abstract class UIViewMonoBehaviour : MonoBehaviour {

    public abstract uint Priority { get; protected set; }
    
    public abstract void AttachModelChangedCallback();
    public abstract void ChangeViewModel(UIViewModel viewModel);
    
    public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);
}

[RequiresAttributeImplementation(typeof(UIViewAttribute))]
public abstract class UIView<TViewModel> : UIViewMonoBehaviour where TViewModel : UIViewModel, new() {

    protected TViewModel viewModel;

    [SerializeField] 
    protected bool ignoreAttachModelChangedCallback;

    public override uint Priority { get; protected set; }
    
    protected virtual void Awake() {
        if (GetType().TryGetCustomAttribute<UIViewAttribute>(out var attribute)) {
            transform.SetSiblingIndex((int)attribute.priority);
            Priority = attribute.priority;
        }

        viewModel ??= new TViewModel();
        
        Service.GetService<UIViewModelLocatorService>().Register(GetViewModelFunc);
    }

    protected virtual void OnEnable() {
        if (viewModel == null) {
            Logger.TraceError($"{nameof(viewModel)} is missing. Create default {typeof(TViewModel).Name}");
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

    public override void AttachModelChangedCallback() {
        if (viewModel.IsAlreadyOnChanged()) {
            viewModel.Clear();
        }
        
        viewModel.OnModelChanged += OnNotifyModelChanged;
    }

    public override void ChangeViewModel(UIViewModel viewModel) {
        if (viewModel is TViewModel castViewModel) {
            this.viewModel?.Dispose();
            this.viewModel = castViewModel;
            if (ignoreAttachModelChangedCallback == false) {
                AttachModelChangedCallback();
            }
            
            this.viewModel.RefreshAll();
        } else {
            throw new InvalidCastException<TViewModel>(viewModel.GetType());
        }
    }

    protected abstract void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args);

    protected virtual TViewModel GetViewModelFunc() {
        viewModel.ThrowIfNull();
        return viewModel;
    }
}