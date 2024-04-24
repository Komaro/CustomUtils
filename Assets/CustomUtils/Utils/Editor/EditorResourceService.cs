using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;

public partial class EditorResourceService : EditorWindow {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorResourceService>("Resource Initializer") : _window;

    private Type _selectProviderType;
    private int _selectProviderTypeIndex;

    private static Type[] _providerTypes = {};
    private static string[] _providerTypeNames = {};
    private static Dictionary<Type, Action> _providerTypeDrawActionDic = new();

    [MenuItem("Service/Resource/Resource Initializer")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorResourceService>()) {
            var providerTypeList = ReflectionManager.GetInterfaceTypes<IResourceProvider>()?.OrderBy(x => x.GetCustomAttribute<ResourceProviderOrderAttribute>()?.order ?? 99).ToList();
            if (providerTypeList?.Any() ?? false) {
                _providerTypes = providerTypeList.ToArray();
                _providerTypeNames = providerTypeList.Select(x => x.Name).ToArray();
            }
            
            _providerTypeDrawActionDic.Clear();
            foreach (var info in Window.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (info.TryGetCustomAttribute<ResourceProviderDrawAttribute>(out var attribute)) {
                    var convertAction = (Action)Delegate.CreateDelegate(typeof(Action), null, info);
                    _providerTypeDrawActionDic.AutoAdd(attribute.providerType, convertAction);
                }
            }
        }
    }
    
    private void OnGUI() {
        if ((_providerTypes?.Any() ?? false) && (_providerTypeNames?.Any() ?? false)) {
            _selectProviderTypeIndex = EditorGUILayout.Popup(_selectProviderTypeIndex, _providerTypeNames);
            if (_selectProviderTypeIndex < _providerTypes.Length && _providerTypes[_selectProviderTypeIndex] != _selectProviderType) {
                _selectProviderType = _providerTypes[_selectProviderTypeIndex];
            }

            if (_providerTypeDrawActionDic.TryGetValue(_selectProviderType, out var action)) {
                action?.Invoke();
                //DrawResourcesProvider();
            }
        } else {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}를 상속받은 Provider를 찾을 수 없습니다.", MessageType.Warning);
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ResourceProviderDrawAttribute : Attribute {
    
    public Type providerType;

    public ResourceProviderDrawAttribute(Type providerType) {
        this.providerType = providerType;
    }
}