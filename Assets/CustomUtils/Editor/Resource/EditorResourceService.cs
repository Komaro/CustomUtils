using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EditorResourceService : EditorService<EditorResourceService> {
    
    private int _selectProviderTypeIndex;
    private Type _selectProviderType;

    private Type[] _providerTypes = { };
    private string[] _providerTypeNames = { };
    
    private int _selectMenuIndex;
    private RESOURCE_SERVICE_MENU_TYPE _selectMenuType;
    
    private readonly Dictionary<(RESOURCE_SERVICE_MENU_TYPE, Type), EditorDrawer> _drawerDic = new();

    private readonly string[] EDITOR_MENUS = EnumUtil.AsSpan<RESOURCE_SERVICE_MENU_TYPE>().ToArray(x => x.ToString());
    private readonly string SELECT_MENU_SAVE_KEY = $"{nameof(EditorResourceService)}_Menu";
    private readonly string SELECT_DRAWER_SAVE_KEY = $"{nameof(EditorResourceService)}_Drawer";

    protected override void OnExitingPlayMode() => Refresh();

    private void OnDestroy() {
        if (_selectProviderType != null && _drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.Close();
            drawer.Destroy();
        }
    }

    protected override void Refresh() {
        if (HasOpenInstances<EditorResourceService>()) {
            var providerTypeList = ReflectionProvider.GetInterfaceTypes<IResourceProvider>()?.OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>()?.priority ?? 999).ToList();
            if (providerTypeList?.Any() ?? false) {
                _providerTypes = providerTypeList.ToArray();
                _providerTypeNames = providerTypeList.Select(x => x.Name).ToArray();
            }

            var typeDefinition = typeof(EditorResourceDrawer<,>);
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

    [MenuItem("Service/Resource/Resource Service")]
    private static void OpenWindow() {
        Window.Show();
        Window.Focus();
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
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorResourceDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }

    private void DrawerCacheRefresh() {
        if (_drawerDic.TryGetValue((_selectMenuType, _selectProviderType), out var drawer)) {
            drawer.CacheRefresh();
        }
    }

    private void DrawerClose() {
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
public abstract class EditorResourceDrawer<TConfig, TNullConfig> : EditorAutoConfigDrawer<TConfig, TNullConfig>
    where TConfig : JsonAutoConfig, new() 
    where TNullConfig : TConfig, new() {

    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorResourceService)}/{CONFIG_NAME}";

    protected EditorResourceDrawer(EditorWindow window) : base(window) { }
}