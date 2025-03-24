using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorResourceService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorResourceService>("Resource Service") : _window;

    private static int _selectProviderTypeIndex;
    private static Type _selectProviderType;

    private static Type[] _providerTypes = { };
    private static string[] _providerTypeNames = { };
    
    private static int _selectMenuIndex;
    private static RESOURCE_SERVICE_MENU_TYPE _selectMenuType;
    
    private static readonly Dictionary<(RESOURCE_SERVICE_MENU_TYPE, Type), EditorDrawer> _drawerDic = new();

    private static readonly string[] EDITOR_MENUS = EnumUtil.GetValues<RESOURCE_SERVICE_MENU_TYPE>().ToArray(x => x.ToString());
    private static readonly string SELECT_MENU_SAVE_KEY = $"{nameof(EditorResourceService)}_Menu";
    private static readonly string SELECT_DRAWER_SAVE_KEY = $"{nameof(EditorResourceService)}_Drawer";

    protected override void OnEditorOpenInitialize() => CacheRefresh();
    protected override void OnExitingPlayMode() => CacheRefresh();

    private void OnDestroy() {
        if (_selectProviderType != null && _drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.Close();
            drawer.Destroy();
        }
    }
    
    [MenuItem("Service/Resource/Resource Service")]
    private static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorResourceService>()) {
            var providerTypeList = ReflectionProvider.GetInterfaceTypes<IResourceProvider>()?.OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>()?.priority ?? 999).ToList();
            if (providerTypeList?.Any() ?? false) {
                _providerTypes = providerTypeList.ToArray();
                _providerTypeNames = providerTypeList.Select(x => x.Name).ToArray();
            }

            var typeDefinition = typeof(EditorAutoConfigResourceDrawer<,>);
            foreach (var type in ReflectionProvider.GetSubTypesOfTypeDefinition(typeDefinition)) {
                if (type.TryGetCustomAttribute<EditorResourceDrawerAttribute>(out var attribute)) {
                    var key = (attribute.menuType, attribute.providerType);
                    if (_drawerDic.TryGetValue(key, out var drawer) == false || drawer == null) {
                        if (SystemUtil.TrySafeCreateInstance(out drawer, type, Window)) {
                            _drawerDic.AutoAdd(key, drawer);
                        }
                    }
                }
            }
            
            if (EditorCommon.TryGet(SELECT_MENU_SAVE_KEY, out RESOURCE_SERVICE_MENU_TYPE menuType)) {
                _selectMenuIndex = (int) menuType;
                _selectMenuType = menuType;
            }
            
            if (EditorCommon.TryGet(SELECT_DRAWER_SAVE_KEY, out string saveProviderName) && _providerTypeNames.TryFindIndex(saveProviderName, out var index)) {
                _selectProviderTypeIndex = index;
                _selectProviderType = _providerTypes[_selectProviderTypeIndex];
            }

            DrawerCacheRefresh();
        }
    }
    
    private void OnGUI() {
        EditorCommon.DrawTopToolbar(ref _selectMenuIndex, index => EditorCommon.Set(SELECT_MENU_SAVE_KEY, index), EDITOR_MENUS);
        
        GUILayout.Space(10f);

        if (_providerTypes?.Any() == false || _providerTypes == null) {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}의 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }
        
        _selectProviderTypeIndex = EditorGUILayout.Popup(_selectProviderTypeIndex, _providerTypeNames);
        if (_selectMenuIndex != (int)_selectMenuType && _selectMenuIndex < EDITOR_MENUS.Length) {
            DrawerClose();
            _selectMenuType = EnumUtil.Convert<RESOURCE_SERVICE_MENU_TYPE>(_selectMenuIndex);
            EditorCommon.Set(SELECT_MENU_SAVE_KEY, _selectMenuType);
            DrawerCacheRefresh();
        } else if (_selectProviderType != _providerTypes[_selectProviderTypeIndex] && _selectProviderTypeIndex < _providerTypes.Length) {
            DrawerClose();
            _selectProviderType = _providerTypes[_selectProviderTypeIndex];
            EditorCommon.Set(SELECT_DRAWER_SAVE_KEY, _selectProviderType.Name);
            DrawerCacheRefresh();
        }

        if (_drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.Draw();
        } else {
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorAutoConfigResourceDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }

    private static void DrawerCacheRefresh() {
        if (_drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.CacheRefresh();
        }
    }

    private static void DrawerClose() {
        if (_drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.Close();
        }
    }
}

public enum RESOURCE_SERVICE_MENU_TYPE {
    Provider,
    Test,
}

[AttributeUsage(AttributeTargets.Class)]
public class EditorResourceDrawerAttribute : Attribute {
    
    public readonly RESOURCE_SERVICE_MENU_TYPE menuType;
    public readonly Type providerType;

    public EditorResourceDrawerAttribute(RESOURCE_SERVICE_MENU_TYPE menuType, Type providerType) {
        if (typeof(IResourceProvider).IsAssignableFrom(providerType)) {
            this.providerType = providerType;
        } else {
            Logger.TraceError($"{providerType.Name} is Invalid {nameof(providerType)}. {nameof(providerType)} must implement from {nameof(IResourceProvider)}.");
            this.providerType = typeof(NullResourceProvider);
        }
        
        this.menuType = menuType;
    }
}

[RequiresAttributeImplementation(typeof(EditorResourceDrawerAttribute))]
public abstract class EditorAutoConfigResourceDrawer<TConfig, TNullConfig> : EditorAutoConfigDrawer<TConfig, TNullConfig>
    where TConfig : JsonAutoConfig, new() 
    where TNullConfig : TConfig, new() {

    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorResourceService)}/{CONFIG_NAME}";

    protected EditorAutoConfigResourceDrawer(EditorWindow window) : base(window) { }
}