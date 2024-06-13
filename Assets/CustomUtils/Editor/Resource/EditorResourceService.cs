using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorResourceService : EditorWindow {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorResourceService>("Resource Service") : _window;

    private static int _selectDrawerTypeIndex;
    private static Type _selectDrawerType;

    private static Type[] _providerTypes = { };
    private static string[] _providerTypeNames = { };
    
    private static Dictionary<Type, EditorResourceDrawer> _providerDrawerDic = new();
    private static Dictionary<Type, EditorResourceDrawer> _testerDrawerDic = new();
    
    private static int _selectMenuIndex;
    private static readonly string[] EDITOR_MENUS = EnumUtil.GetValues<EDITOR_TYPE>().Select(x => x.ToString()).ToArray();
    
    private static readonly string SELECT_MENU_SAVE_KEY = $"{nameof(EditorResourceService)}_Menu";
    private static readonly string SELECT_DRAWER_SAVE_KEY = $"{nameof(EditorResourceService)}_Drawer";

    [MenuItem("Service/Resource/Resource Service")]
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
            
            foreach (var type in ReflectionManager.GetSubClassTypes<EditorResourceDrawer>()) {
                if (type.TryGetCustomAttribute<EditorResourceProviderDrawerAttribute>(out var attribute) && TrySelectDrawerDic(attribute, out var drawerDic)) {
                    if (drawerDic.TryGetValue(attribute.providerType, out var drawer) == false || drawer == null) {
                        drawerDic.Add(attribute.providerType, Activator.CreateInstance(type) as EditorResourceDrawer);
                    }
                }
            }

            _selectMenuIndex = EditorCommon.TryGet(SELECT_MENU_SAVE_KEY, out int index) && index < EDITOR_MENUS.Length ? index : 0;
            if (EditorCommon.TryGet(SELECT_DRAWER_SAVE_KEY, out string saveProviderName) && _providerTypeNames.TryFindIndex(saveProviderName, out index)) {
                _selectDrawerTypeIndex = index;
                _selectDrawerType = _providerTypes[_selectDrawerTypeIndex];
            }
            
            if (_selectDrawerType != null && TrySelectDrawerDic(_selectMenuIndex, out var selectDrawerDic) && selectDrawerDic.TryGetValue(_selectDrawerType, out var selectDrawer)) {
                selectDrawer.CacheRefresh();
            }
        }
    }
    
    private void OnGUI() {
        EditorCommon.DrawTopToolbar(ref _selectMenuIndex, index => EditorCommon.SetInt(SELECT_MENU_SAVE_KEY, index), EDITOR_MENUS);
        
        GUILayout.Space(10f);

        if (_providerTypes?.Any() == false || _providerTypes == null) {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}를 상속받은 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }

        if (TrySelectDrawerDic(_selectMenuIndex, out var drawerDic)) {
            _selectDrawerTypeIndex = EditorGUILayout.Popup(_selectDrawerTypeIndex, _providerTypeNames);
            if (_selectDrawerTypeIndex < _providerTypes.Length && _providerTypes[_selectDrawerTypeIndex] != _selectDrawerType) {
                if (drawerDic.TryGetValue(_selectDrawerType, out var closeDrawer)) {
                    closeDrawer.Close();
                }
                
                _selectDrawerType = _providerTypes[_selectDrawerTypeIndex];
                EditorCommon.SetString(SELECT_DRAWER_SAVE_KEY, _selectDrawerType.ToString());
                
                if (drawerDic.TryGetValue(_selectDrawerType, out var openDrawer)) {
                    openDrawer.CacheRefresh();
                }
            }

            if (drawerDic.TryGetValue(_selectDrawerType, out var drawer)) {
                drawer.Draw();
            } else {
                EditorGUILayout.HelpBox($"유효한 {nameof(EditorResourceDrawer)} 를 찾을 수 없습니다.", MessageType.Warning);
            }
        } else {
            EditorGUILayout.HelpBox($"{nameof(EDITOR_TYPE)}에 맞는 Drawer를 찾는데 실패하였습니다.", MessageType.Error);
        }
    }

    private void OnDestroy() {
        if (_selectDrawerType != null && _providerDrawerDic.TryGetValue(_selectDrawerType, out var drawer)) {
            drawer.Destroy();
        }
    }

    private static bool TrySelectDrawerDic(Attribute attribute, out Dictionary<Type, EditorResourceDrawer> drawerDic) {
        drawerDic = SelectDrawerDic(attribute);
        return drawerDic != null;
    }

    private static Dictionary<Type, EditorResourceDrawer> SelectDrawerDic(Attribute attribute) => attribute switch {
        EditorResourceTesterDrawerAttribute => _testerDrawerDic,
        EditorResourceProviderDrawerAttribute => _providerDrawerDic,
        _ => null
    };

    private static bool TrySelectDrawerDic(int index, out Dictionary<Type, EditorResourceDrawer> drawerDic) {
        drawerDic = SelectDrawerDic(index);
        return drawerDic != null;
    }
    
    private static Dictionary<Type, EditorResourceDrawer> SelectDrawerDic(int index) => EnumUtil.ConvertInt<EDITOR_TYPE>(_selectMenuIndex) switch {
        EDITOR_TYPE.Provider => _providerDrawerDic,
        EDITOR_TYPE.Test => _testerDrawerDic,
        _ => null
    };

    private enum EDITOR_TYPE {
        Provider,
        Test,
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EditorResourceProviderDrawerAttribute : Attribute {
    
    public Type providerType;

    public EditorResourceProviderDrawerAttribute(Type providerType) => this.providerType = providerType;
}

[AttributeUsage(AttributeTargets.Class)]
public class EditorResourceTesterDrawerAttribute : EditorResourceProviderDrawerAttribute {
    
    public EditorResourceTesterDrawerAttribute(Type providerType) : base(providerType) { }
}

public abstract class EditorResourceDrawer {
    
    public virtual void Close() { }
    public virtual void Destroy() { }
    public abstract void CacheRefresh();
    public abstract void Draw();
}