using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;

public class EditorResourceService : EditorWindow {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorResourceService>("Resource Initializer") : _window;

    private static int _selectProviderTypeIndex;
    private static Type _selectProviderType;

    private static Type[] _providerTypes = {};
    private static string[] _providerTypeNames = {};
    private static Dictionary<Type, EditorResourceProviderDrawer> _providerDrawerDic = new();

    [MenuItem("Service/Resource/Resource Provider Initializer")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorResourceService>()) {
            var providerTypeList = ReflectionManager.GetInterfaceTypes<IResourceProvider>()?.OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>()?.order ?? 99).ToList();
            if (providerTypeList?.Any() ?? false) {
                _providerTypes = providerTypeList.ToArray();
                _providerTypeNames = providerTypeList.Select(x => x.Name).ToArray();
            }
            
            _providerDrawerDic.Clear();
            foreach (var type in ReflectionManager.GetSubClassTypes<EditorResourceProviderDrawer>()) {
                if (type.TryGetCustomAttribute<EditorResourceProviderDrawerAttribute>(out var attribute)) {
                    if (_providerDrawerDic.ContainsKey(type) == false || _providerDrawerDic[type] == null) {
                        _providerDrawerDic.Add(attribute.providerType, Activator.CreateInstance(type) as EditorResourceProviderDrawer);
                    }
                }
            }

            if (_selectProviderType != null && _providerDrawerDic.TryGetValue(_selectProviderType, out var drawer)) {
                drawer.CacheRefresh();
            }
        }
    }
    
    private void OnGUI() {
        if ((_providerTypes?.Any() ?? false) && (_providerTypeNames?.Any() ?? false)) {
            _selectProviderTypeIndex = EditorGUILayout.Popup(_selectProviderTypeIndex, _providerTypeNames);
            if (_selectProviderTypeIndex < _providerTypes.Length && _providerTypes[_selectProviderTypeIndex] != _selectProviderType) {
                _selectProviderType = _providerTypes[_selectProviderTypeIndex];
            }

            if (_providerDrawerDic.TryGetValue(_selectProviderType, out var drawer)) {
                drawer.Draw();
            } else {
                EditorGUILayout.HelpBox($"유효한 {nameof(EditorResourceProviderDrawer)} 를 찾을 수 없습니다.", MessageType.Warning);
            }
        } else {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}를 상속받은 Provider를 찾을 수 없습니다.", MessageType.Warning);
        }
    }
}

public abstract class EditorResourceProviderDrawer {
    public abstract void CacheRefresh();
    public abstract void Draw();
}

[AttributeUsage(AttributeTargets.Class)]
public class EditorResourceProviderDrawerAttribute : Attribute {
    
    public Type providerType;

    public EditorResourceProviderDrawerAttribute(Type providerType) {
        this.providerType = providerType;
    }
}