using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorBuildService : EditorService<EditorBuildService> {
    
    private PopupDrawer<Type> _builderPopupDrawer;

    private readonly Dictionary<Type, EditorDrawer> _drawerDic = new();
    
    private readonly string SELECT_DRAWER_KEY = $"{nameof(EditorBuildService)}_{nameof(EditorDrawer)}";

    [MenuItem("Service/Build/Build Service")]
    public static void OpenWindow() => Window.Show();

    protected override void Refresh() {
        if (HasOpenInstances<EditorBuildService>()) {
            var builderTypes = ReflectionProvider.GetSubTypesOfType<BuilderBase>().OrderBy(type => type.TryGetCustomAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999).ToArray();
            if (builderTypes.Length > 0) {
                var builderTypeNames = builderTypes.Select(type => type.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : type.Name).ToArray();
                if (EditorCommon.TryGet(SELECT_DRAWER_KEY, out string builderName) == false || builderTypeNames.TryFindIndex(builderName, out var selectIndex) == false) {
                    selectIndex = 0;
                }

                _builderPopupDrawer = new PopupDrawer<Type>(selectIndex, string.Empty, builderTypeNames, builderTypes, OnChangeBuilderType);
            }
            
            foreach (var type in ReflectionProvider.GetSubTypesOfTypeDefinition(typeof(EditorBuildDrawer<,>))) {
                if (type.TryGetCustomAttribute<EditorBuildDrawerAttribute>(out var attribute) && SystemUtil.TrySafeCreateInstance<EditorDrawer>(out var drawer, type, Window)) {
                    if (_drawerDic.TryAdd(attribute.builderType, drawer) == false) {
                        Logger.TraceLog($"{attribute.builderType} {nameof(EditorDrawer)} is already instantiated");
                    }
                }
            }

            DrawerCacheRefresh(_builderPopupDrawer.SelectValue);
        }
    }
    
    private void OnGUI() {
        if (_builderPopupDrawer.ValueCount <= 0) {
            EditorGUILayout.HelpBox($"{nameof(BuilderBase)}를 상속받은 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }
        
        _builderPopupDrawer.Draw();

        GUILayout.Space(10f);

        if (_drawerDic.TryGetValue(_builderPopupDrawer.SelectValue, out var drawer)) {
            drawer?.Draw();
            EditorCommon.DrawSeparator();
        } else {
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorBuildDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }
    
    private void DrawerCacheRefresh(Type type) {
        if (_drawerDic.TryGetValue(type, out var drawer)) {
            drawer?.CacheRefresh();
        }
    }

    private void DrawerClose(Type type) {
        if (_drawerDic.TryGetValue(type, out var drawer)) {
            drawer?.Close();
        }
    }

    private void OnChangeBuilderType(Type previousType, Type selectedType) {
        DrawerClose(previousType);
        EditorCommon.Set(SELECT_DRAWER_KEY, _builderPopupDrawer.SelectOption);
        DrawerCacheRefresh(selectedType);
    }
}