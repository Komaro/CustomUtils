using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorResourceService : EditorService<EditorResourceService> {
    
    private readonly GlobalEnum<EditorResourceServiceMenuEnum> _menuTypes = new();
    private string[] _menuTypeNames;
    
    private readonly GlobalEnum<EditorResourceServiceTypeEnum> _serviceTypes = new();
    private string[] _serviceTypeNames;
    
    private readonly Dictionary<(Enum, Enum), EditorDrawer> _drawerDic = new();

    private readonly string SELECT_MENU_SAVE_KEY = $"{nameof(EditorResourceService)}_Menu";
    private readonly string SELECT_DRAWER_SAVE_KEY = $"{nameof(EditorResourceService)}_Drawer";

    protected override void OnExitingPlayMode() => Refresh();

    private void OnDestroy() {
        if (_menuTypes != null && _serviceTypes != null && _drawerDic.TryGetValue((_menuTypes.Value, _serviceTypes.Value), out var drawer)) {
            drawer.Close();
            drawer.Destroy();
        }
    }

    protected override void Refresh() {
        if (HasOpenInstances<EditorResourceService>()) {
            var typeDefinition = typeof(EditorResourceDrawer<,>);
            foreach (var type in ReflectionProvider.GetSubTypesOfTypeDefinition(typeDefinition)) {
                if (type.TryGetCustomAttribute<EditorResourceDrawerAttribute>(out var attribute)) {
                    var key = (_menuType:attribute.menuType, _serviceType:attribute.resourceType);
                    if (_drawerDic.TryGetValue(key, out var drawer) == false || drawer == null) {
                        if (SystemUtil.TrySafeCreateInstance(out drawer, type, Window)) {
                            _drawerDic.AutoAdd(key, drawer);
                        }
                    }
                }
            }

            if (EditorCommon.TryGet(SELECT_MENU_SAVE_KEY, out int menuIndex)) {
                _menuTypes.Index = menuIndex;
            }

            if (EditorCommon.TryGet(SELECT_DRAWER_SAVE_KEY, out int serviceIndex)) {
                _serviceTypes.Index = serviceIndex;
            }
            
            _menuTypeNames = _menuTypes.Select(enumValue => enumValue.ToString()).ToArray();
            _serviceTypeNames = _serviceTypes.Select(enumValue => enumValue.ToString()).ToArray();
            
            DrawerRefresh();
        }
    }

    [MenuItem("Service/Resource/Resource Service")]
    private static void OpenWindow() {
        Window.Show();
        Window.Focus();
    }

    private void OnGUI() {
        EditorCommon.DrawTopToolbar(_menuTypes.Index, index => {
            DrawerClose();
            _menuTypes.Index = index;
            EditorCommon.Set(SELECT_MENU_SAVE_KEY, index);
            DrawerRefresh();
        }, _menuTypeNames);
        
        GUILayout.Space(10f);

        if (_serviceTypes is not { Count: > 0 }) {
            EditorGUILayout.HelpBox($"{nameof(IResourceProvider)}의 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }

        using (var scope = new EditorGUI.ChangeCheckScope()) {
            _serviceTypes.Index = EditorGUILayout.Popup(_serviceTypes.Index, _serviceTypeNames);
            if (scope.changed) {
                DrawerClose();
                EditorCommon.Set(SELECT_DRAWER_SAVE_KEY, _serviceTypes.Index);
                DrawerRefresh();
            }
        }
        
        if (_drawerDic.TryGetValue((_menuTypes.Value, _serviceTypes.Value), out var drawer)) {
            drawer.Draw();
        } else {
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorResourceDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }

    private void DrawerRefresh() {
        if (_drawerDic.TryGetValue((_menuTypes.Value, _serviceTypes.Value), out var drawer)) {
            drawer.CacheRefresh();
        }
    }

    private void DrawerClose() {
        if (_drawerDic.TryGetValue((_menuTypes.Value, _serviceTypes.Value), out var drawer)) {
            drawer.Close();
        }
    }
}


public class EditorResourceServiceMenuEnum : PriorityAttribute { }
public class EditorResourceServiceTypeEnum : PriorityAttribute { }


[EditorResourceServiceMenuEnum]
public enum RESOURCE_SERVICE_MENU_TYPE {
    Provider,
    Test,
}

[EditorResourceServiceTypeEnum]
public enum RESOURCE_SERVICE_TYPE {
    Resources,
    AssetBundle,
    Addressable,
}