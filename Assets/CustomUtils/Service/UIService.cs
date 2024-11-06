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

    void IService.Stop() {

    }

    void IService.Remove() {
        // TODO. 순회 처리 필요
        if (_uiRoot != null) {
            Object.Destroy(_uiRoot);
        }

        if (_uiGlobalRoot != null) {
            Object.Destroy(_uiGlobalRoot);
        }
    }

    public TUIView Change<TUIView>(UIViewModel viewModel) where TUIView : class, IUIView {
        if (_uiCallStack.TryPop(out var oldUIView)) {
            Close(oldUIView);
        }
        
        return Open<TUIView>(viewModel);
    }

    public TUIView Change<TUIView>() where TUIView : class, IUIView {
        if (_uiCallStack.TryPop(out var oldUIView)) {
            Close(oldUIView);
        }

        return Open<TUIView>();
    }

    public TUIView Open<TUIView>(UIViewModel viewModel) where TUIView : class, IUIView {
        var uiView = Open<TUIView>();
        if (uiView != null) {
            uiView.ChangeViewModel(viewModel);
            return uiView;
        }

        return null;
    }

    public TUIView Open<TUIView>() where TUIView : class, IUIView {
        if (TryGetUI<TUIView>(out var uiView)) {
            _uiCallStack.Push(uiView);
            uiView.SetActive(true);
            return uiView;
        }
        
        return null;
    }

    public void Close() {
        if (_uiCallStack.Count <= 1) {
            Logger.TraceError("The UI can no longer be closed");
            return;
        }
        
        if (_uiCallStack.TryPop(out var uiView)) {
            Close(uiView);
        }
    }

    private void Close<TUIView>() where TUIView : class, IUIView {
        if (TryGetUI<TUIView>(out var uiView)) {
            Close(uiView);
        }
    }

    private void Close(IUIView uiView) {
        uiView.SetActive(false);
    }

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

    // TODO. 최적화 필요
    // TODO. TryInstantiate 확장 픽요 
    private IUIView CreateUIViewSwitch(Type type) {
        if (_viewAttributeDic.TryGetValue(type, out var attribute) && Service.GetService<ResourceService>().TryInstantiate(attribute.prefab, out var go) && go.TryGetComponent<IUIView>(out var uiView)) {
            if (type.IsDefined<GlobalUIAttribute>()) {
                uiView.transform.SetParent(_uiGlobalRoot);
                _cachedGlobalUIDic.TryAdd(type, uiView);
            } else {
                uiView.transform.SetParent(_uiRoot);
                _cachedUIDic.TryAdd(type, uiView);
            }
        }
        
        return null;
    }
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