using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIService : IService {

    private UIInitializeProvider _initializeProvider;

    private Transform _uiRoot;
    private Transform _uiGlobalRoot;

    void IService.Init() {
        foreach (var type in ReflectionProvider.GetSubClassTypes<UIInitializeProvider>().OrderBy(type => type.TryGetCustomInheritedAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999)) {
            if (SystemUtil.TrySafeCreateInstance<UIInitializeProvider>(type, out var provider) && provider.IsValid()) {
                _initializeProvider = provider;
            }
        }

        if (_initializeProvider == null) {
            Logger.TraceError($"{nameof(_initializeProvider)} is null. {nameof(UIService)} initialize failed");
        }
    }
    
    void IService.Start() {
        _uiRoot ??= _initializeProvider.CreateUIRoot();
        _uiGlobalRoot ??= _initializeProvider.CreateGlobalUIRoot();
    }

    void IService.Stop() {

    }

    void IService.Remove() {
        
        
        
        
        
        if (_uiRoot != null) {
            Object.Destroy(_uiRoot);
        }

        if (_uiGlobalRoot != null) {
            Object.Destroy(_uiGlobalRoot);
        }
    }
}

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class UIInitializeProvider {

    public abstract bool IsValid();
    public abstract Transform CreateUIRoot();

    public virtual Transform CreateGlobalUIRoot() {
        var go = new GameObject("UIGlobal");
        Object.DontDestroyOnLoad(go);
        return go.transform;
    }
}

[Priority(100)]
public class TestUIInitializeProvider : UIInitializeProvider {

    public override bool IsValid() => true;

    public override Transform CreateUIRoot() {
        var go = new GameObject("UI");
        return go.transform;
    }
}