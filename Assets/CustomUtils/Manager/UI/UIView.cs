using System;
using System.Collections.Immutable;
using UnityEngine;

public interface IUIView {

    public void ChangeModel(UIViewModel viewModel);
}

public abstract class UIView<TViewModel> : MonoBehaviour, IUIView where TViewModel : UIViewModel, new() {

    protected TViewModel model = new();


    protected virtual void OnEnable() => Attach();

    public virtual void ChangeModel(UIViewModel viewModel) => model = (TViewModel) viewModel;
    protected virtual void Attach() => model.OnModelChanged += OnNotifyModelChanged;

    protected abstract void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args);
}

public abstract class UIViewModel {

    private static readonly ImmutableHashSet<Type> NOTIFY_TYPE_SET = ReflectionProvider.GetSubClassTypes(typeof(NotifyField)).ToImmutableHashSet();

    public delegate void NotifyModelChangeHandler(string fieldName, NotifyFieldChangedEventArgs args);
    public SafeDelegate<NotifyModelChangeHandler> OnModelChanged;

    public UIViewModel() {
        foreach (var fieldInfo in GetType().GetFields()) {
            if (NOTIFY_TYPE_SET.Contains(fieldInfo.FieldType.GetGenericTypeDefinition()) && fieldInfo.GetValue(this) is NotifyField notifyField) {
                notifyField.OnChanged += args => OnModelChanged.handler?.Invoke(fieldInfo.Name, args);
            }
        }
    }
}