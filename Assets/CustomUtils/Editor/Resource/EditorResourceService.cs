using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorResourceService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorResourceService>("Resource Service") : _window;

    private static int _selectDrawerTypeIndex;
    private static Type _selectDrawerType;

    private static Type[] _providerTypes = { };
    private static string[] _providerTypeNames = { };
    
    private static int _selectMenuIndex;
    private static RESOURCE_SERVICE_MENU_TYPE _selectMenuType;
    
    private static Dictionary<(RESOURCE_SERVICE_MENU_TYPE, Type), EditorResourceDrawer> _drawerDic = new();

    private static readonly string[] EDITOR_MENUS = EnumUtil.GetValues<RESOURCE_SERVICE_MENU_TYPE>().Select(x => x.ToString()).ToArray();
    private static readonly string SELECT_MENU_SAVE_KEY = $"{nameof(EditorResourceService)}_Menu";
    private static readonly string SELECT_DRAWER_SAVE_KEY = $"{nameof(EditorResourceService)}_Drawer";

    protected override void OnEditorOpenInitialize() => CacheRefresh();

    private void OnDestroy() {
        if (_selectDrawerType != null && _drawerDic.TryGetValue((_selectMenuType, _selectDrawerType), out var drawer)) {
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
            var providerTypeList = ReflectionManager.GetInterfaceTypes<IResourceProvider>()?.OrderBy(x => x.GetCustomAttribute<ResourceProviderAttribute>()?.order ?? 99).ToList();
            if (providerTypeList?.Any() ?? false) {
                _providerTypes = providerTypeList.ToArray();
                _providerTypeNames = providerTypeList.Select(x => x.Name).ToArray();
            }
            
            foreach (var type in ReflectionManager.GetSubClassTypes<EditorResourceDrawer>()) {
                if (type.TryGetCustomAttribute<EditorResourceDrawerAttribute>(out var attribute)) {
                    var key = (attribute.menuType, attribute.providerType);
                    if (_drawerDic.TryGetValue(key, out var drawer) == false || drawer == null) {
                        _drawerDic.Add(key, Activator.CreateInstance(type) as EditorResourceDrawer);
                    }
                }
            }

            if (EditorCommon.TryGet(SELECT_MENU_SAVE_KEY, out RESOURCE_SERVICE_MENU_TYPE menuType)) {
                _selectMenuIndex = (int) menuType;
                _selectMenuType = menuType;
            }
            
            if (EditorCommon.TryGet(SELECT_DRAWER_SAVE_KEY, out string saveProviderName) && _providerTypeNames.TryFindIndex(saveProviderName, out var index)) {
                _selectDrawerTypeIndex = index;
                _selectDrawerType = _providerTypes[_selectDrawerTypeIndex];
            }

            DrawerCacheRefresh();
        }
    }
    
    private void OnGUI() {
        EditorCommon.DrawTopToolbar(ref _selectMenuIndex, index => EditorCommon.Set(SELECT_MENU_SAVE_KEY, index), EDITOR_MENUS);
        
        GUILayout.Space(10f);

        if (_providerTypes?.Any() == false || _providerTypes == null) {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}를 상속받은 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }
        
        _selectDrawerTypeIndex = EditorGUILayout.Popup(_selectDrawerTypeIndex, _providerTypeNames);
        if (_selectMenuIndex != (int)_selectMenuType && _selectMenuIndex < EDITOR_MENUS.Length) {
            DrawerClose();
            _selectMenuType = EnumUtil.ConvertInt<RESOURCE_SERVICE_MENU_TYPE>(_selectMenuIndex);
            EditorCommon.Set(SELECT_MENU_SAVE_KEY, _selectMenuType);
            DrawerCacheRefresh();
        } else if (_selectDrawerType != _providerTypes[_selectDrawerTypeIndex] && _selectDrawerTypeIndex < _providerTypes.Length) {
            DrawerClose();
            _selectDrawerType = _providerTypes[_selectDrawerTypeIndex];
            EditorCommon.Set(SELECT_DRAWER_SAVE_KEY, _selectDrawerType.Name);
            DrawerCacheRefresh();
        }

        if (_drawerDic.TryGetValue((_selectMenuType, _selectDrawerType), out var drawer)) {
            drawer.Draw();
        } else {
            EditorGUILayout.HelpBox($"유효한 {nameof(EditorResourceDrawer)} 를 찾을 수 없습니다.", MessageType.Warning);
        }
    }

    private static void DrawerCacheRefresh() {
        if (_drawerDic.TryGetValue((_selectMenuType, _selectDrawerType), out var drawer)) {
            drawer.CacheRefresh();
        }
    }

    private static void DrawerClose() {
        if (_drawerDic.TryGetValue((_selectMenuType, _selectDrawerType), out var drawer)) {
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
            Logger.TraceError($"{providerType.Name} is Invalid {nameof(providerType)}. {nameof(providerType)} must inherit from {nameof(IResourceProvider)}.");
            this.providerType = typeof(NullResourceProvider);
        }
        
        this.menuType = menuType;
    }
}

public abstract class EditorResourceDrawer {
    
    public virtual void Close() { }
    public virtual void Destroy() { }
    public abstract void CacheRefresh();
    public abstract void Draw();
}

public abstract class EditorResourceDrawerAutoConfig<TConfig, TNullConfig> : EditorResourceDrawer
    where TConfig : JsonAutoConfig, new() 
    where TNullConfig : TConfig, new() {

    protected TConfig config;
    protected SystemWatcherServiceOrder watcherOrder;
    
    protected abstract string CONFIG_NAME { get; }
    protected abstract string CONFIG_PATH { get; }

    protected EditorResourceDrawerAutoConfig() => watcherOrder = CreateWatcherOrder();

    public override void Close() {
        if (config?.IsNull() == false) {
            config.StopAutoSave();
            config.Save(CONFIG_PATH);
        }
        
        Service.GetService<SystemWatcherService>().StopWatcher(watcherOrder);
    }

    public override void Destroy() {
        if (watcherOrder != null) {
            Service.GetService<SystemWatcherService>().RemoveWatcher(watcherOrder);
        }
    }

    public override void Draw() {
        if (config?.IsNull() ?? true) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 설정이 저장되지 않으며 일부 기능을 사용할 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button($"{CONFIG_NAME} 파일 생성")) {
                EditorCommon.OpenCheckDialogue($"{CONFIG_NAME} 파일 생성", $"{CONFIG_NAME} 파일을 생성합니다.\n경로는 아래와 같습니다.\n{CONFIG_PATH}", ok: () => {
                    if ((config = config.Clone<TConfig>()) != null) {
                        config.Save(CONFIG_PATH);
                        config.StartAutoSave(CONFIG_PATH);
                    }
                });
            }

            EditorCommon.DrawSeparator();
        } else {
            GUILayout.Space(10f);
        }
    }

    protected SystemWatcherServiceOrder CreateWatcherOrder() => new(Constants.Path.COMMON_CONFIG_FOLDER, CONFIG_NAME, OnSystemWatcherEventHandler);
    
    protected virtual void OnSystemWatcherEventHandler(object ob, FileSystemEventArgs args) {
        if (config == null) {
            Logger.TraceError($"{nameof(config)} is Null.");
            return;
        }
        
        switch (args.ChangeType) {
            case WatcherChangeTypes.Created:
                if (config.IsNull()) {
                    config = config.Clone<TConfig>();
                }
                break;
            case WatcherChangeTypes.Deleted:
                if (config.IsNull() == false) {
                    config = config.Clone<TNullConfig>();
                }
                break;
        }
    }
}