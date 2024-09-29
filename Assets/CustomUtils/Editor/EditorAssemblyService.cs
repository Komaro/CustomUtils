using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

public class EditorAssemblyService : EditorService {
    
    private static EditorWindow _window;
    private static EditorWindow Window => _window == null ? _window = GetWindow<EditorAssemblyService>("Assembly Service") : _window;

    private static AssemblyTreeView _assemblyTreeView;
    
    [MenuItem("Service/Assembly Service")]
    private static void OpenWindow() {
        Window.Show();
        CacheRefresh();
        Window.Focus();
    }
    
    [DidReloadScripts]
    private static void CacheRefresh() {
        if (HasOpenInstances<EditorAssemblyService>()) {
            _assemblyTreeView ??= new AssemblyTreeView();
            _assemblyTreeView.Clear();
            
            foreach (var assembly in UnityAssemblyProvider.GetUnityAssemblySet().Where(assembly => assembly.name.StartsWith("Unity") == false)) {
                _assemblyTreeView.Add(assembly);
            }
            
            _assemblyTreeView.Reload();
        }
    }
    
    protected override void OnEditorOpenInitialize() => CacheRefresh();

    private void OnGUI() {
        _assemblyTreeView?.Draw();
    }
}

public class AssemblyTreeView : EditorServiceTreeView {

    private static readonly MultiColumnHeaderState.Column[] COLUMNS = {
        CreateColumn("No", 35f, 35f),
        CreateColumn("이름", 100f),
        CreateColumn("네임스페이스", 100f)
    };
    
    public AssemblyTreeView() : base(COLUMNS) { }

    protected override void RowGUI(RowGUIArgs args) {
        if (args.item is AssemblyTreeViewItem item) {
            EditorGUI.LabelField(args.GetCellRect(0), item.id.ToString(), Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(1), item.assembly.name, Constants.Draw.CENTER_LABEL);
            EditorGUI.LabelField(args.GetCellRect(2), item.assembly.rootNamespace, Constants.Draw.CENTER_LABEL);
            
            if (args.rowRect.Contains(Event.current.mousePosition) && Event.current.clickCount > 1 && AssetDatabaseUtil.TryFindAssets<AssemblyDefinitionAsset>(out var enumerable, item.assembly.name, false)) {
                var asset = enumerable.FirstOrDefault();
                if (asset != null) {
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }
    }

    public void Add(Assembly assembly) => itemList.Add(new AssemblyTreeViewItem(itemList.Count, assembly));

    protected sealed class AssemblyTreeViewItem : TreeViewItem {
        
        public Assembly assembly;

        public AssemblyTreeViewItem(int id, Assembly assembly) {
            this.id = id;
            this.assembly = assembly;
        }
    }
}