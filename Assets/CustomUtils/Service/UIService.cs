using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIService : IService {

    private UIInitializeProvider _initializeProvider;

    private Transform _uiRoot;
    private Transform _uiGlobalRoot;
    
    private ConcurrentStack<IUIView> _uiCallStack = new();

    public IUIView Current => _uiCallStack.TryPeek(out var uiView) ? uiView : null;
    
    public IUIView Previous {
        get {
            if (_uiCallStack.TryPop(out var uiView)) {
                if (_uiCallStack.TryPeek(out var previousUIView)) {
                    _uiCallStack.Push(uiView);
                    return previousUIView;
                }
                
                _uiCallStack.Push(uiView);
                return uiView;
            }

            return null;
        }
    }

    private readonly Dictionary<Type, IUIView> _cachedUIDic = new();
    private readonly Dictionary<Type, IUIView> _cachedGlobalUIDic = new();

    private ImmutableHashSet<Type> _viewSet;
    private ImmutableDictionary<Type, UIViewAttribute> _viewAttributeDic;

    void IService.Init() {
        foreach (var type in ReflectionProvider.GetSubClassTypes<UIInitializeProvider>().OrderBy(type => type.TryGetCustomInheritedAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999)) {
            if (SystemUtil.TryCreateInstance<UIInitializeProvider>(out var provider, type) && provider.IsReady()) {
                _initializeProvider = provider;
            }
        }
        
        if (_initializeProvider == null) {
            Logger.TraceError($"{nameof(_initializeProvider)} is null. {nameof(UIService)} initialize failed");
        }

        _viewSet = ReflectionProvider.GetSubClassTypeDefinitions(typeof(UIView<>)).ToImmutableHashSet();
        _viewAttributeDic = _viewSet.ToImmutableDictionary(type => type, type => type.GetCustomAttribute<UIViewAttribute>());
    }
    
    void IService.Start() {
        _uiRoot ??= _initializeProvider.GetUIRoot();
        _uiGlobalRoot ??= _initializeProvider.GetGlobalUIRoot();
    }

    void IService.Stop() { }

    void IService.Remove() {
        // TODO. 순회 처리 필요
        if (_uiRoot != null) {
            Object.Destroy(_uiRoot);
        }

        if (_uiGlobalRoot != null) {
            Object.Destroy(_uiGlobalRoot);
        }
    }

    public bool TryChange<TUIView>(UIViewModel viewModel, out TUIView uiView) where TUIView : class, IUIView => (uiView = Change<TUIView>(viewModel)) != null;

    public TUIView Change<TUIView>(UIViewModel viewModel) where TUIView : class, IUIView {
        if (typeof(TUIView) == Current?.GetType()) {
            Current.ChangeViewModel(viewModel);
            return Current as TUIView;
        }
        
        if (TryChange<TUIView>(out var uiView)) {
            uiView.ChangeViewModel(viewModel);
            return uiView;
        }

        return null;
    }

    public bool TryChange<TUIView>(out TUIView uiView) where TUIView : class, IUIView => (uiView = Change<TUIView>()) != null;

    public TUIView Change<TUIView>() where TUIView : class, IUIView {
        if (typeof(TUIView) == Current?.GetType()) {
            Logger.TraceLog($"{typeof(TUIView).Name} is already open", Color.yellow);
            return null;
        }
    
        if (_uiCallStack.TryPop(out var oldUIView)) {
            Close(oldUIView);
        }

        return Open<TUIView>();
    }
    
    public bool TryReturn(UIViewModel viewModel, out IUIView uiView) => (uiView = Return(viewModel)) != null;

    public IUIView Return(UIViewModel viewModel) {
        if (TryReturn(out var previousUIView)) {
            previousUIView.ChangeViewModel(viewModel);
            return previousUIView;
        }
        
        return null;
    }
    
    public bool TryReturn(out IUIView previousUIView) => (previousUIView = Return()) != null;

    public IUIView Return() {
        if (_uiCallStack.TryPop(out var currentUIView)) {
            if (_uiCallStack.TryPeek(out var previousUIView)) {
                Close(currentUIView);
                previousUIView.SetActive(true);
                return previousUIView;
            }
            
            _uiCallStack.Push(currentUIView);
        }
        
        return null;
    }

    public bool TryOpen<TUIView>(UIViewModel viewModel, out TUIView uiView) where TUIView : class, IUIView => (uiView = Open<TUIView>(viewModel)) != null;

    public TUIView Open<TUIView>(UIViewModel viewModel) where TUIView : class, IUIView {
        if (typeof(TUIView) == Current?.GetType()) {
            Current.ChangeViewModel(viewModel);
            return Current as TUIView;
        }
        
        if (TryOpen<TUIView>(out var uiView)) {
            uiView.ChangeViewModel(viewModel);
            return uiView;
        }
    
        return null;
    }

    public bool TryOpen<TUIView>(out TUIView uiView) where TUIView : class, IUIView => (uiView = Open<TUIView>()) != null;

    public TUIView Open<TUIView>() where TUIView : class, IUIView {
        if (typeof(TUIView) == Current?.GetType()) {
            Logger.TraceLog($"{typeof(TUIView).Name} is already open", Color.yellow);
            return null;
        }
        
        if (TryGetUI<TUIView>(out var uiView)) {
            _uiCallStack.Push(uiView);
            uiView.SetActive(true);
            return uiView;
        }
        
        return null;
    }

    public void Close() {
        if (_uiCallStack.TryPop(out var uiView)) {
            Close(uiView);
        }
    }

    public void Close<TUIView>() where TUIView : class, IUIView {
        if (TryGetUI<TUIView>(out var uiView)) {
            Close(uiView);
        }
    }

    private void Close(IUIView uiView) => uiView.SetActive(false);

    private bool TryGetUI<TUIView>(out TUIView uiView) where TUIView : class, IUIView => (uiView = GetUI<TUIView>()) != null;

    private TUIView GetUI<TUIView>() where TUIView : class, IUIView {
        if (_viewSet.Contains(typeof(TUIView)) == false) {
            throw new ArgumentException($"{nameof(TUIView)} is an invalid type of {typeof(UIView<>).Name}");
        }

        if (TryGetUIViewSwitch(typeof(TUIView), out var uiView)) {
            return uiView as TUIView;
        }

        if (TryCreateUIViewSwitch(typeof(TUIView), out uiView)) {
            return uiView as TUIView;
        }
        
        return null;
    }

    private bool TryGetUIViewSwitch(Type type, out IUIView uiView) => (uiView = GetUIViewSwitch(type)) != null;

    private IUIView GetUIViewSwitch(Type type) {
        if (_cachedUIDic.TryGetValue(type, out var uiView)) {
            return uiView;
        }

        if (_cachedGlobalUIDic.TryGetValue(type, out uiView)) {
            return uiView;
        }

        return null;
    }

    private bool TryCreateUIViewSwitch(Type type, out IUIView uiView) => (uiView = CreateUIViewSwitch(type)) != null;
    
    private IUIView CreateUIViewSwitch(Type type) {
        if (_viewAttributeDic.TryGetValue(type, out var attribute) && Service.GetService<ResourceService>().TryInstantiate(out var go, attribute.prefab) && go.TryGetComponent<IUIView>(out var uiView)) {
            if (type.IsDefined<GlobalUIAttribute>()) {
                uiView.transform.SetParent(_uiGlobalRoot);
                _cachedGlobalUIDic.TryAdd(type, uiView);
            } else {
                uiView.transform.SetParent(_uiRoot);
                _cachedUIDic.TryAdd(type, uiView);
            }

            return uiView;
        }

        return null;
    }

    public bool IsValid() => _uiRoot != null && _uiGlobalRoot != null || _viewSet.IsEmpty == false || _viewAttributeDic.IsEmpty == false;
}

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class UIInitializeProvider {

    public abstract bool IsReady();

    public abstract Transform GetUIRoot();

    public virtual Transform GetGlobalUIRoot() {
        var go = new GameObject("UIGlobal");
        Object.DontDestroyOnLoad(go);
        return go.transform;
    }
}

public class GlobalUIAttribute : PriorityAttribute { }


// TODO. 현 구조상 Exception을 추가하려면 구조를 새롭게 수정해야 함.
public class AlreadyOpenUIException : Exception {
    
    public AlreadyOpenUIException(Type type) : base($"{type.Name} is already open ui") { }
}

public class AlreadyOpenUIException<T> : Exception {
    
    public AlreadyOpenUIException() : base($"{typeof(T).Name} is already open ui") { }
}

public class CreateFailedUIException : Exception {

    public CreateFailedUIException(Type type) : base($"Failed to create {type.Name}") { }
}

public class CreateFailedUIException<T> : Exception {
    
    public CreateFailedUIException() : base($"Failed to create {typeof(T).Name}") { }
} 