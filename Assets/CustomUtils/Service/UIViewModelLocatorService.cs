using System;
using System.Collections.Generic;

public class UIViewModelLocatorService : IService {

    private readonly Dictionary<Type, UIViewModelResolver> _resolverDic = new();

    void IService.Start() { }
    void IService.Stop() { }

    public void Register<TViewModel>(Func<TViewModel> accessor) where TViewModel : UIViewModel => _resolverDic.AddOrUpdate(typeof(TViewModel), () => new UIViewModelResolver(accessor), (_, handler) => handler.Update(accessor));
    public void Release<TViewModel>() where TViewModel : UIViewModel => _resolverDic.AutoRemove(typeof(TViewModel));

    public bool TryGetViewModel<TViewModel>(out TViewModel viewModel) where TViewModel : UIViewModel => (viewModel = GetViewModel<TViewModel>()) != null; 
    public TViewModel GetViewModel<TViewModel>() where TViewModel : UIViewModel => GetViewModelResolver<TViewModel>()?.Resolve<TViewModel>();
    
    public bool TryGetViewModelResolver<TViewModel>(out UIViewModelResolver handler) where TViewModel : UIViewModel => (handler = GetViewModelResolver<TViewModel>()) != null;
    public UIViewModelResolver GetViewModelResolver<TViewModel>() where TViewModel : UIViewModel => _resolverDic.TryGetValue(typeof(TViewModel), out var resolver) ? resolver : null;

    public UIViewModelAccessor<TViewModel> GetViewModelAccessor<TViewModel>() where TViewModel : UIViewModel => _resolverDic.TryGetValue(typeof(TViewModel), out var resolver) ? new UIViewModelAccessor<TViewModel>(resolver) : default;
}

public class UIViewModelResolver {

    private Func<UIViewModel> _accessor;

    public UIViewModelResolver(Func<UIViewModel> accessor) => _accessor = accessor;

    public UIViewModelResolver Update(Func<UIViewModel> accessor) {
        _accessor = accessor;
        return this;
    }
    
    public TViewModel Resolve<TViewModel>() where TViewModel : UIViewModel => _accessor?.Invoke() as TViewModel;
}

public readonly ref struct UIViewModelAccessor<TViewModel> where TViewModel : UIViewModel {

    private readonly UIViewModelResolver _resolver;

    public TViewModel ViewModel => _resolver?.Resolve<TViewModel>();

    public UIViewModelAccessor(UIViewModelResolver resolver) => _resolver = resolver;

    public bool IsValid() => _resolver != null;
}