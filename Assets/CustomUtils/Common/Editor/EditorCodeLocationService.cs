using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[Obsolete("Not Implement")]
// 리다이렉션 기능을 구현하기 위해 너무 많은 작업 소요가 발생. 근본적인 문제 해결에 너무 많은 작업 소요가 발생하여 현 시점에서 중단할 피요가 있음
public partial class EditorCodeLocationService : EditorService<EditorCodeLocationService> {

    private static bool _isActiveAnalyze;
    private static bool _isActiveFullProcessor;

    private readonly BoolSwitchText _fullProcessInfoText = new($"사용 가능한 모든 프로세서를 사용합니다\n프로세서의 갯수 : {Environment.ProcessorCount}", $"사용 가능한 모든 프로세서 중 절반만 사용합니다\n프로세서의 갯수 : {Environment.ProcessorCount / 2}");
    
    private SerializedAssemblyArray _ignoreAssemblyAssets;

    private AsyncCustomOperation _operation;
    
    private const string DEFAULT_PATH = "Service/TypeLocation";
    private const string WINDOW_PATH = DEFAULT_PATH + "/Type Location Service";
    
    private const string ACTIVE_ANALYZE = DEFAULT_PATH + "ActiveAnalzye";
    private const string ACTIVE_FULL_PROCESSOR = DEFAULT_PATH + "ActiveFullProcessor";

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad() {
        // TODO. CodeLocationService로 옮길지 결정 필요
        // AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    // TODO. CodeLocationService로 옮길지 결정 필요
    // private static void OnAfterAssemblyReload() {
    //     if (_isActiveAnalyze) {
    //         AnalyzeTypeLocation();
    //     }
    // }

    [MenuItem(WINDOW_PATH)]
    private static void OpenWindow() => Window.Open();
    
    protected override void Refresh() => _operation = AsyncRefresh();

    protected override async Task AsyncRefresh(AsyncCustomOperation operation, CancellationToken token) {
        await operation.ToTask();
        token.ThrowIfCancellationRequested();
        
        if (Service.TryGetService<CodeLocationService>(out var service)) {
            _isActiveAnalyze = service.IsActiveAnalyze;
            _isActiveFullProcessor = service.IsActiveFullProcessor;
        }
    }

    private void OnGUI() {
        EditorGUILayout.Space(10);
        
        if (_operation is { IsDone: false }) {
            EditorCommon.DrawProgressBar(_operation, "분석중...");
            Repaint();
            return;
        }

        EditorCommon.DrawLabelToggle(ref _isActiveAnalyze, "타입 분석 활성화", () => EditorPrefsUtil.Set(ACTIVE_ANALYZE, _isActiveAnalyze));
        if (_isActiveAnalyze == false) {
            EditorGUILayout.HelpBox("타입 분석이 활성화 되어있지 않습니다", MessageType.Warning);
            return;
        }

        EditorCommon.DrawLabelToggle(ref _isActiveFullProcessor, "전체 프로세서 사용", () => {
            EditorPrefsUtil.Set(ACTIVE_FULL_PROCESSOR, _isActiveFullProcessor);
            _fullProcessInfoText.SwitchValue = _isActiveFullProcessor;
        });
        
        EditorGUILayout.HelpBox(_fullProcessInfoText, MessageType.Info);

        EditorCommon.DrawSeparator();
        
        if (GUILayout.Button("타입 재분석", GUILayout.Height(50f))) {
            // TODO. Operation 처리
            // _operation = Service.RefreshOperationService(....);
            Service.RestartService<CodeLocationService>();
            Refresh();
        }
        
        if (_ignoreAssemblyAssets.Value != null) {
            EditorGUILayout.LabelField("어셈블리 제외 리스트", Constants.Draw.AREA_TITLE_STYLE);
            _ignoreAssemblyAssets.Draw();
        }
        
        if (_operation.IsDone) {
            EditorCommon.DrawSeparator();
        } else {
            EditorGUILayout.HelpBox("타입을 다시 분석하는 중입니다...", MessageType.Warning);
        }
        
        EditorGUI.EndDisabledGroup();
    }

    private class SerializedAssemblyArray : SerializedArray<AssemblyDefinitionAsset> {

        public const string IGNORE_ASSEMBLIES_KEY = "IGNORE_ASSEMBLIES";

        protected override void OnRemove(ReorderableList list) {
            base.OnRemove(list);
            Save();
        }

        protected override void OnEnable() {
            base.OnEnable();
            Load();
        }

        public void Save() => EditorPrefsUtil.Set(IGNORE_ASSEMBLIES_KEY, Value.Select(asset => asset.name));

        public void Load() {
            if (EditorPrefsUtil.TryGet(IGNORE_ASSEMBLIES_KEY, out string[] ignoreAssemblies)) {
                Value = AssetDatabaseUtil.FindAssets<AssemblyDefinitionAsset>(FilterUtil.CreateFilter(AreaFilter.all)).Where(asset => ignoreAssemblies.Contains(asset.name)).ToArray();
            }
            
            serializedObj.Update();
        }

        protected override void OnChanged(AssemblyDefinitionAsset value, int index) => Save();
    }
}