using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

public class EditorAssemblyService : EditorService {

    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorAssemblyService>("Assembly Service") : _window;
    
    private static ImmutableList<TextAsset> _assemblyDefinitionList;

    [MenuItem("Service/AssemblyService")]
    private static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorAssemblyService>()) {
            // TODO. Here
            _assemblyDefinitionList = AssetDatabaseUtil.FindAssets<TextAsset>(FilterUtil.CreateFilter(TypeFilter.AssemblyDefinitionAsset, AreaFilter.assets)).ToImmutableList();
        }
    }
    
    protected override void OnEditorOpenInitialize() {
        
    }

    private void OnGUI() {
        if (_assemblyDefinitionList != null) {
            if (GUILayout.Button("Test")) {
                Logger.TraceLog(_assemblyDefinitionList.ToStringCollection(asset => asset.name));
            }
        }
    }

    private class AssemblyDefinition {
        
    }
}