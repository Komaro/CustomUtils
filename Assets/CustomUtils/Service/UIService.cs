using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

// TODO. 중복 생성이 가능한 UI의 경우 해당 Service를 통해 처리할 수 없음 여기서는 어디까지나 단일 UI만 처리 가능. 이를 해결하기 위해 새로운 형태의 Service가 필요함 ex) MultiUIService.cs
public class UIService : IService {

    private UIInitializeProvider _initializeProvider;

    private Transform _uiRoot;
    private Transform _uiGlobalRoot;
    
    private readonly CallStack<UIViewMonoBehaviour> _uiCallStack = new();

    public UIViewMonoBehaviour Current => _uiCallStack.TryPeek(out var uiView) ? uiView : null;
    public UIViewMonoBehaviour Previous => _uiCallStack.TryPeek(out var uiView, 1) ? uiView : null;

    private readonly Dictionary<Type, UIViewMonoBehaviour> _cachedUIDic = new();
    private readonly Dictionary<Type, UIViewMonoBehaviour> _cachedGlobalUIDic = new();

    private ImmutableHashSet<Type> _viewSet;
    private ImmutableDictionary<Type, UIViewAttribute> _viewAttributeDic;

    void IService.Init() => AttachInitializeProvider();

    void IService.Start() {
        _uiRoot ??= _initializeProvider.GetUIRoot();
        _uiGlobalRoot ??= _initializeProvider.GetGlobalUIRoot();
    }

    void IService.Stop() { }

    void IService.Refresh() => AttachInitializeProvider();

    void IService.Remove() {
        _cachedUIDic.Clear();
        _cachedGlobalUIDic.Clear();
        
        if (_uiRoot != null) {
            Object.Destroy(_uiRoot.gameObject);
        }

        if (_uiGlobalRoot != null) {
            Object.Destroy(_uiGlobalRoot.gameObject);
        }
    }

    private void AttachInitializeProvider() {
        foreach (var type in ReflectionProvider.GetSubTypesOfType<UIInitializeProvider>().OrderByDescending(type => type.TryGetCustomInheritedAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999)) {
            if (SystemUtil.TryCreateInstance<UIInitializeProvider>(out var provider, type) && provider.IsReady()) {
                _initializeProvider = provider;
            }
        }
        
        if (_initializeProvider == null) {
            throw new NullReferenceException($"{nameof(_initializeProvider)} is null. {nameof(UIService)} initialize failed");
        }

        _viewSet = ReflectionProvider.GetSubTypesOfTypeDefinition(typeof(UIView<>)).ToImmutableHashSet();
        _viewAttributeDic = _viewSet.ToImmutableDictionary(type => type, type => type.GetCustomAttribute<UIViewAttribute>());
    }
    
    public void ChangeViewModel<TUIView>(UIViewModel viewModel) where TUIView : UIViewMonoBehaviour {
        if (TryGetUI<TUIView>(out var uiView)) {
            uiView.ChangeViewModel(viewModel);
        }
    }

    public bool TryChange<TUIView>(UIViewModel viewModel, out TUIView uiView) where TUIView : UIViewMonoBehaviour => (uiView = Change<TUIView>(viewModel)) != null;

    public TUIView Change<TUIView>(UIViewModel viewModel) where TUIView : UIViewMonoBehaviour {
        if (Current != null && typeof(TUIView) == Current.GetType()) {
            Current.ChangeViewModel(viewModel);
            return Current as TUIView;
        }
        
        if (TryChange<TUIView>(out var uiView)) {
            uiView.ChangeViewModel(viewModel);
            return uiView;
        }

        return null;
    }

    public bool TryChange<TUIView>(out TUIView uiView) where TUIView : UIViewMonoBehaviour => (uiView = Change<TUIView>()) != null;

    public TUIView Change<TUIView>() where TUIView : UIViewMonoBehaviour {
        if (Current != null && typeof(TUIView) == Current.GetType()) {
            Logger.TraceLog($"{typeof(TUIView).Name} is already open", Color.yellow);
            return null;
        }
    
        if (_uiCallStack.TryPop(out var oldUIView)) {
            Close(oldUIView);
        }

        return Open<TUIView>();
    }
    
    public bool TryReturn(UIViewModel viewModel, out UIViewMonoBehaviour uiView) => (uiView = Return(viewModel)) != null;

    public UIViewMonoBehaviour Return(UIViewModel viewModel) {
        if (TryReturn(out var previousUIView)) {
            previousUIView.ChangeViewModel(viewModel);
            return previousUIView;
        }
        
        return null;
    }
    
    public bool TryReturn(out UIViewMonoBehaviour previousUIView) => (previousUIView = Return()) != null;

    public UIViewMonoBehaviour Return() {
        if (_uiCallStack.Count < 2) {
            return null;
        }

        if (_uiCallStack.TryPop(out var currentUIView) && _uiCallStack.TryPeek(out var previousUIView)) {
            Close(currentUIView);
            previousUIView.SetActive(true);
            return previousUIView;
        }
        
        return null;
    }

    public bool TryOpen<TUIView>(UIViewModel viewModel, out TUIView uiView) where TUIView : UIViewMonoBehaviour => (uiView = Open<TUIView>(viewModel)) != null;

    public TUIView Open<TUIView>(UIViewModel viewModel) where TUIView : UIViewMonoBehaviour {
        if (Current != null && typeof(TUIView) == Current.GetType()) {
            Current.ChangeViewModel(viewModel);
            return Current as TUIView;
        }
        
        if (TryOpen<TUIView>(out var uiView)) {
            uiView.ChangeViewModel(viewModel);
            return uiView;
        }
    
        return null;
    }

    public bool TryOpen<TUIView>(out TUIView uiView) where TUIView : UIViewMonoBehaviour => (uiView = Open<TUIView>()) != null;

    public TUIView Open<TUIView>() where TUIView : UIViewMonoBehaviour {
        if (Current != null && typeof(TUIView) == Current.GetType()) {
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

    public void Close<TUIView>() where TUIView : UIViewMonoBehaviour {
        if (TryGetUI<TUIView>(out var uiView)) {
            Close(uiView);
        }
    }

    private void Close(UIViewMonoBehaviour uiView) => uiView.SetActive(false);

    private bool TryGetUI<TUIView>(out TUIView uiView) where TUIView : UIViewMonoBehaviour => (uiView = GetUI<TUIView>()) != null;

    private TUIView GetUI<TUIView>() where TUIView : UIViewMonoBehaviour {
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

    private bool TryGetUIViewSwitch(Type type, out UIViewMonoBehaviour uiView) => (uiView = GetUIViewSwitch(type)) != null;

    private UIViewMonoBehaviour GetUIViewSwitch(Type type) => _cachedUIDic.TryGetValue(type, out var uiView) || _cachedGlobalUIDic.TryGetValue(type, out uiView) ? uiView : null;

    private bool TryCreateUIViewSwitch(Type type, out UIViewMonoBehaviour uiView) => (uiView = CreateUIViewSwitch(type)) != null;
    
    private UIViewMonoBehaviour CreateUIViewSwitch(Type type) {
        if (_viewAttributeDic.TryGetValue(type, out var attribute) && Service.GetService<ResourceService>().TryInstantiate(out var go, attribute.prefab) && go.TryGetOrAddComponent<UIViewMonoBehaviour>(type, out var uiView)) {
            if (uiView.transform.TryGetRectTransform(out var rect)) {
                var offsetMax = rect.offsetMax;
                var offsetMin = rect.offsetMin;
                
                if (attribute.isGlobal) {
                    uiView.transform.SetParent(_uiGlobalRoot);
                    _cachedGlobalUIDic.TryAdd(type, uiView);
                } else {
                    uiView.transform.SetParent(_uiRoot);
                    _cachedUIDic.TryAdd(type, uiView);
                }

                rect.offsetMax = offsetMax;
                rect.offsetMin = offsetMin;

                uiView.SetActive(false);
                return uiView;
            }
        }

        return null;
    }

    public bool IsValid() => _uiRoot != null && _uiGlobalRoot != null && _viewSet.IsEmpty == false && _viewAttributeDic.IsEmpty == false;
}

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class UIInitializeProvider {

    public abstract bool IsReady();

    public abstract Transform GetUIRoot();

    public virtual Transform GetGlobalUIRoot() {
        var go = new GameObject("UIGlobal");
        go.GetOrAddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        Object.DontDestroyOnLoad(go);
        return go.transform;
    }
}