using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class EditorCachingService : EditorService {
    
    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorCachingService>("CachingService") : _window;

    private static CachingService _service;
    private static List<Cache> _cacheList = new();

    private string _createDirectoryName;
    
    private Vector2 _cachingScrollViewPosition;

    protected override void OnEditorOpenInitialize() => CacheRefresh();
    
    [MenuItem("Service/Caching Service")]
    private static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }

    [DidReloadScripts(99999)]
    private static void CacheRefresh() {
        if (Service.TryGetService(out _service)) {
            _cacheList = _service.GetAllCacheList();
        }
    }

    private void OnGUI() {
        GUILayout.Space(10);
        EditorCommon.DrawLabelTextField("현재 활성화된 Cache", Caching.currentCacheForWriting.path, 180f);
        EditorCommon.DrawSeparator();
        if (_cacheList.Any()) {
            GUILayout.BeginVertical("box");
            _cachingScrollViewPosition = EditorGUILayout.BeginScrollView(_cachingScrollViewPosition, false, false, GUILayout.MaxHeight(300));
            Cache? removeCache = null;
            foreach (var cache in _cacheList) {
                using (new GUILayout.HorizontalScope()) {
                    if (GUILayout.Button("X", GUILayout.MaxWidth(30), GUILayout.MinWidth(30))) {
                        removeCache = cache;
                    }
                    
                    if (GUILayout.Button("Open", GUILayout.MaxWidth(45), GUILayout.MinWidth(45))) {
                        EditorUtility.RevealInFinder(cache.path);
                    }

                    if (GUILayout.Button("Clear", GUILayout.MaxWidth(45), GUILayout.MinWidth(45))) {
                        _service.Clear(cache);
                    }
                    
                    if (GUILayout.Button(cache.path)) {
                        _service.Set(cache);
                    }
                }
            }

            if (removeCache.HasValue) {
                _service.Remove(removeCache.Value);
                CacheRefresh();
            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        _createDirectoryName = EditorCommon.DrawLabelTextField("폴더명", _createDirectoryName);
        if (GUILayout.Button("Caching 추가", GUILayout.Height(50)) && string.IsNullOrEmpty(_createDirectoryName) == false) {
            _service.Add(_createDirectoryName);
            CacheRefresh();
        }
    }
}
