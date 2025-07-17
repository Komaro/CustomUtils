using System;
using System.Collections.Generic;

public class UIViewModelLocatorService : IService {

    private readonly Dictionary<Type, UIViewModelHandler> _handlerDic = new();

    void IService.Start() { }
    void IService.Stop() { }

    public void Register<TViewModel>(Func<TViewModel> accessor) where TViewModel : UIViewModel => _handlerDic.AddOrUpdate(typeof(TViewModel), () => new UIViewModelHandler(accessor), (_, handler) => handler.UpdateAccessor(accessor));
    public void Release<TViewModel>() where TViewModel : UIViewModel => _handlerDic.AutoRemove(typeof(TViewModel));

    public bool TryGetViewModel<TViewModel>(out TViewModel viewModel) where TViewModel : UIViewModel => (viewModel = GetViewModel<TViewModel>()) != null; 
    public TViewModel GetViewModel<TViewModel>() where TViewModel : UIViewModel => GetViewModelHandler<TViewModel>()?.GetViewModel<TViewModel>();
    
    public bool TryGetViewModelHandler<TViewModel>(out UIViewModelHandler handler) where TViewModel : UIViewModel => (handler = GetViewModelHandler<TViewModel>()) != null;
    public UIViewModelHandler GetViewModelHandler<TViewModel>() where TViewModel : UIViewModel => _handlerDic.TryGetValue(typeof(TViewModel), out var handler) ? handler : null;

    public UIViewModelAccessor<TViewModel> GetViewModelAccessor<TViewModel>() where TViewModel : UIViewModel => _handlerDic.TryGetValue(typeof(TViewModel), out var handler) ? new UIViewModelAccessor<TViewModel>(handler) : default;
}

public class UIViewModelHandler {

    private Func<UIViewModel> _accessor;

    public UIViewModelHandler(Func<UIViewModel> accessor) => _accessor = accessor;

    public UIViewModelHandler UpdateAccessor(Func<UIViewModel> accessor) {
        _accessor = accessor;
        return this;
    }
    
    public TViewModel GetViewModel<TViewModel>() where TViewModel : UIViewModel => _accessor?.Invoke() as TViewModel;
}

public readonly ref struct UIViewModelAccessor<TViewModel> where TViewModel : UIViewModel {

    private readonly UIViewModelHandler _handler;
    
    public TViewModel ViewModel => _handler.GetViewModel<TViewModel>();
    
    public UIViewModelAccessor(UIViewModelHandler handler) => _handler = handler;

    public bool IsValid() => _handler != null;
}