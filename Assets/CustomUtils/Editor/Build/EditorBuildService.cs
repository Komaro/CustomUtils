using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public partial class EditorBuildService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorBuildService>("Build Service") : _window;
    
    private static int _selectBuilderIndex;
    private static Type _selectBuilderType;
    
    private static Type[] _builderTypes;
    private static string[] _builderTypeNames;

    private static readonly Dictionary<Type, EditorDrawer> _drawerDic = new();
    
    private static readonly string SELECT_DRAWER_KEY = $"{nameof(EditorBuildService)}_{nameof(EditorDrawer)}";

    protected override void OnEditorOpenInitialize() => CacheRefresh();
    
    [MenuItem("Service/Build/Build Service")]
    public static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts]
    public static void CacheRefresh() {
        if (HasOpenInstances<EditorBuildService>()) {
            _builderTypes = ReflectionProvider.GetSubClassTypes<Builder>().OrderBy(type => type.TryGetCustomAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999).ToArray();
            if (_builderTypes.Any()) {
                _builderTypeNames = _builderTypes.Select(type => type.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : type.Name).ToArray();
            }
            
            foreach (var type in ReflectionProvider.GetSubClassTypeDefinitions(typeof(EditorBuildDrawer<,>))) {
                if (type.TryGetCustomAttribute<EditorBuildDrawerAttribute>(out var attribute) && SystemUtil.TrySafeCreateInstance<EditorDrawer>(out var drawer, type, Window)) {
                    if (_drawerDic.ContainsKey(attribute.builderType) == false) {
                        _drawerDic.AutoAdd(attribute.builderType, drawer);
                    }
                }
            }
            
            if (EditorCommon.TryGet(SELECT_DRAWER_KEY, out string builderName) == false || _builderTypeNames.TryFindIndex(builderName, out _selectBuilderIndex) == false) {
                _selectBuilderIndex = Math.Clamp(_selectBuilderIndex, 0, _builderTypes.Length);
            }

            if (_builderTypes.Length > 0) {
                _selectBuilderType = _builderTypes[_selectBuilderIndex];
            }
            
            DrawerCacheRefresh();
        }
    }

    private void OnGUI() {
        if (_builderTypes == null || _builderTypes.Any() == false) {
            EditorGUILayout.HelpBox($"{nameof(Builder)}를 상속받은 구현이 존재하지 않습니다.", MessageType.Error);
            return;
        }
        
        _selectBuilderIndex = EditorGUILayout.Popup(_selectBuilderIndex, _builderTypeNames);

        GUILayout.Space(10f);

        if (_selectBuilderIndex < _builderTypes.Length && _selectBuilderType != _builderTypes[_selectBuilderIndex]) {
            DrawerClose();
            _selectBuilderType = _builderTypes[_selectBuilderIndex];
            EditorCommon.Set(SELECT_DRAWER_KEY, _builderTypeNames[_selectBuilderIndex]);
            DrawerCacheRefresh();
        }
        
        DrawDrawer();
    }
}