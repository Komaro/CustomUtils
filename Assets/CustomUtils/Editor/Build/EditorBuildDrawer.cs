using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[RequiresAttributeImplementation(typeof(EditorBuildDrawerAttribute))]
public abstract class EditorBuildDrawer<TConfig, TNullConfig> : EditorAutoConfigDrawer<TConfig, TNullConfig> 
    where TConfig : BuildConfig, new() 
    where TNullConfig : TConfig, new() {

    private string _buildInfoMemo;
    
    protected readonly Type builderType;
    protected readonly BuildTarget buildTarget;
    protected readonly BuildTargetGroup buildTargetGroup;
    
    protected ToggleDraw[] defineSymbols = {};
    
    private Vector2 _editorWindowScrollViewPosition;
    private Vector2 _defineSymbolScrollViewPosition;
    private bool _activateSceneFoldOut;
    private bool _sceneAssetFoldOut;
    private Vector2 _buildInfoMemoScrollViewPosition;
    
    protected static HashSet<string> buildOptionSet;
    protected static Dictionary<string, EditorBuildSettingsScene> activateSceneDic = new();
    protected static Dictionary<string, EditorAssetInfo<SceneAsset>> sceneAssetInfoDic = new();

    protected override string CONFIG_NAME => $"{typeof(TConfig).Name}{Constants.Extension.JSON}";
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorBuildService)}/{CONFIG_NAME}";

    protected EditorBuildDrawer(EditorWindow window) : base(window) {
        var type = GetType();
        if (type.TryGetCustomAttribute<EditorBuildDrawerAttribute>(out var drawerAttribute)) {
            builderType = drawerAttribute.builderType;
            if (builderType.TryGetCustomAttribute<BuilderAttribute>(out var builderAttribute)) {
                buildTarget = builderAttribute.buildTarget;
                buildTargetGroup = builderAttribute.buildTargetGroup;
            }
        }
    }

    public override void CacheRefresh() {
        base.CacheRefresh();
        
        if (ReflectionProvider.TryGetAttributeEnumTypes<DefineSymbolEnumAttribute>(out var types)) {
            var configDefineSymbolSet = config.defineSymbols.Split(Constants.Separator.DEFINE_SYMBOL).ToHashSet();
            defineSymbols = types.SelectMany(type => Enum.GetValues(type).Cast<Enum>().Select(enumValue => {
                var name = enumValue.ToString();
                return type.TryGetFieldInfo(out var info, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) && info.TryGetCustomAttribute<EnumValueAttribute>(out var attribute)
                    ? new ToggleDraw(name, configDefineSymbolSet.Contains(name), attribute.header)
                    : new ToggleDraw(name, configDefineSymbolSet.Contains(name));
            })).ToArray();
        }
        
        buildOptionSet = ReflectionProvider.GetAttributeEnumSets<BuildOptionEnumAttribute>()
            .Where(info => info.attribute.buildTargetGroup == BuildTargetGroup.Unknown || info.attribute.buildTargetGroup == buildTargetGroup)
            .SelectMany(info => Enum.GetValues(info.enumType).Cast<object>()).Select(ob => ob.ToString()).ToHashSetWithDistinct();
        
        RefreshScenes();
    }

    public override void Draw() {
        base.Draw();
        if (config != null) {
            _editorWindowScrollViewPosition = EditorGUILayout.BeginScrollView(_editorWindowScrollViewPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
                EditorGUILayout.HelpBox($"현재 활성화된 {nameof(BuildTarget)}({EditorUserBuildSettings.activeBuildTarget})과 {nameof(BuilderBase)}의 {nameof(BuildTarget)}({buildTarget})이 일치하지 않습니다", MessageType.Warning);
                if (GUILayout.Button($"{nameof(BuildTarget)} 전환\n[{EditorUserBuildSettings.activeBuildTarget} ==> {buildTarget}]")) {
                    SwitchPlatform();
                }
            }
            
            using (new GUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawFolderOpenSelector("빌드 폴더", "선택", ref config.buildDirectory);
            }
            
            EditorCommon.DrawSeparator();
            
            GUILayout.Label("빌드 옵션", Constants.Draw.AREA_TITLE_STYLE);
            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawLabelTextFieldWithRefresh(nameof(config.applicationIdentifier), config.applicationIdentifier, () => config.applicationIdentifier = PlayerSettings.applicationIdentifier);
                EditorCommon.DrawLabelTextField(nameof(config.bundleVersion), ref config.bundleVersion, 150f);
            }
            
            DrawStackTrace();
            DrawBuildOptions();
            
            EditorCommon.DrawSeparator();
            DrawDefineSymbol();
            
            EditorCommon.DrawSeparator();
            DrawCustomOption();
            
            EditorCommon.DrawSeparator();
            DrawScene();

            EditorCommon.DrawSeparator();
            DrawBuildButton();
            
            EditorCommon.DrawSeparator();
            DrawBuildResultRecord();
        
            EditorGUILayout.EndScrollView();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawStackTrace() {
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            GUILayout.Label("스택 트레이스 (Stack Trace)", Constants.Draw.BOLD_CENTER_LABEL);
            using (new EditorGUILayout.HorizontalScope()) {
                foreach (var traceType in EnumUtil.AsSpan<StackTraceLogType>()) {
                    if (GUILayout.Button(traceType.ToString())) {
                        foreach (var logType in EnumUtil.AsSpan<LogType>()) {
                            config.stackTraceDic[logType] = traceType;
                        }
                    }
                }
            }
            
            foreach (var logType in EnumUtil.AsSpan<LogType>()) {
                using (new EditorGUILayout.HorizontalScope()) {
                    config.stackTraceDic[logType] = EditorCommon.DrawEnumPopup(logType.ToString(), config.stackTraceDic[logType], 60f);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawBuildOptions() {
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            EditorCommon.DrawLabelToggle(ref config.developmentBuild, nameof(config.developmentBuild), 150f);
            EditorCommon.DrawLabelToggle(ref config.autoConnectProfile, nameof(config.autoConnectProfile), 150f);
            EditorCommon.DrawLabelToggle(ref config.deepProfilingSupport, nameof(config.deepProfilingSupport), 150f);
            EditorCommon.DrawLabelToggle(ref config.scriptDebugging, nameof(config.scriptDebugging), 150f);
        } 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawDefineSymbol() {
        GUILayout.Label("디파인 심볼 (Define Symbols)", Constants.Draw.AREA_TITLE_STYLE);
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            _defineSymbolScrollViewPosition = EditorGUILayout.BeginScrollView(_defineSymbolScrollViewPosition, GUILayout.ExpandWidth(true), GUILayout.MinHeight(100f), GUILayout.MaxHeight(250f));
            EditorCommon.DrawToggleBox(defineSymbols, () => config.defineSymbols = string.Join(Constants.Separator.DEFINE_SYMBOL, defineSymbols.Where(x => x.isActive)));
            EditorGUILayout.EndScrollView();
            
            EditorCommon.DrawLabelTextFieldWithRefresh("Define Symbol", config.defineSymbols, () => {
                config.defineSymbols = buildTargetGroup.GetScriptingDefineSymbolsForGroup();
                var defineSymbolSet = config.defineSymbols.Split(Constants.Separator.DEFINE_SYMBOL).ToHashSet();
                defineSymbols.ForEach(symbol => symbol.isActive = defineSymbolSet.Contains(symbol.name));
            });
            
            EditorGUILayout.HelpBox($"새로고침시 {nameof(PlayerSettings)}의 Define Symbol 값으로 {nameof(BuildConfig)}의 값을 갱신합니다", MessageType.Warning);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawCustomOption() {
        GUILayout.Label("커스텀 옵션", Constants.Draw.AREA_TITLE_STYLE);
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            foreach (var buildOption in buildOptionSet) {
                if (config.optionDic.ContainsKey(buildOption)) {
                    config.optionDic[buildOption] = EditorCommon.DrawLabelToggle(config.optionDic[buildOption], buildOption, 200f);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawScene() {
        if (EditorCommon.DrawLabelButton("씬 (Scene)", Constants.Draw.REFRESH_ICON, Constants.Draw.AREA_TITLE_STYLE)) {
            RefreshScenes();
        }

        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            _activateSceneFoldOut = EditorGUILayout.BeginFoldoutHeaderGroup(_activateSceneFoldOut, "활성화 된 씬");
            if (_activateSceneFoldOut) {
                foreach (var (path, scene) in activateSceneDic) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.Space(15f, false);
                        if (EditorCommon.DrawFitToggle(scene.enabled) != scene.enabled) {
                            scene.enabled = scene.enabled == false;
                            EditorBuildSettings.scenes = activateSceneDic.Values.ToArray();
                        }
                        
                        EditorGUILayout.TextField(path);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            if (_activateSceneFoldOut) {
                EditorGUILayout.Space(15f);
            }

            _sceneAssetFoldOut = EditorGUILayout.BeginFoldoutHeaderGroup(_sceneAssetFoldOut, "프로젝트 전체 씬 에셋");
            if (_sceneAssetFoldOut) {
                foreach (var (path, info) in sceneAssetInfoDic) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.Space(15f, false);
                        EditorCommon.DrawLabelTextField(info.Name, path);
                    }
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawBuildButton() {
        GUILayout.Label("빌드 (Build)", Constants.Draw.AREA_TITLE_STYLE);
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            EditorCommon.DrawLabelToggle(ref config.isLogBuildReport, "빌드 결과 기록", 150f);
        }
        
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            if (config.isLogBuildReport) {
                EditorGUILayout.Space(5f);
                EditorCommon.DrawWideTextArea("메모", ref _buildInfoMemo);
            }
            
            EditorCommon.DrawLabelTextField("빌더 빌드 타겟", buildTarget.ToString());
            EditorCommon.DrawLabelTextField("현재 빌드 타겟", EditorUserBuildSettings.activeBuildTarget.ToString());

            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorGUILayout.HelpBox($"Editor가 현재 컴파일 혹은 {nameof(AssetDatabase)} 업데이트중에 있습니다", MessageType.Error);
            } else {
                if (string.IsNullOrEmpty(config.buildDirectory) == false) {
                    if (GUILayout.Button("Build", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.Height(45f))) {
                        if (EditorBuildSettings.scenes.Any(scene => scene.enabled) == false) {
                            EditorUtility.DisplayDialog("경고", $"{nameof(EditorBuildSettings)}에 활성화 된 씬이 존재하지 않습니다.", "확인");
                            return;
                        }
                    
                        if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
                            EditorCommon.ShowCheckDialogue($"{nameof(buildTarget)} miss match", $"현재 선택된 {nameof(BuildTarget)}({buildTarget})과 활성화된 {nameof(BuildTarget)}({EditorUserBuildSettings.activeBuildTarget})이 동일하지 않습니다. 플랫폼 전환 후 빌드를 진행합니다", ok: () => Build());
                        } else {
                            EditorCommon.ShowCheckDialogue("빌드", "빌드를 진행합니다.\n" +
                                                                 $"{EditorUserBuildSettings.selectedBuildTargetGroup}\n{EditorUserBuildSettings.activeBuildTarget}\n\n" +
                                                                 $"Define Symbol : {config.defineSymbols}\n\n" +
                                                                 $"대상 디렉토리 : {config.buildDirectory}\n\n" +
                                                                 $"활성화된 옵션\n\t{config.optionDic.ToStringCollection("\n\t")}", ok: () => Build(_buildInfoMemo));
                        }
                    }
                } else {
                    EditorGUILayout.HelpBox("빌드를 내보낼 폴더가 선택되지 않았습니다", MessageType.Error);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawBuildResultRecord() {
        if (config.GetRecordCount() > 0) {
            GUILayout.Label("빌드 결과", Constants.Draw.AREA_TITLE_STYLE);
            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                DrawBuildResultRecordCursorNavigator();
                if (config.GetRecordCount() <= 0) {
                    return;
                }
                
                var info = config[config.Cursor];
                EditorCommon.DrawLabelTextField("결과", info.result.ToString());
                EditorCommon.DrawLabelTextField("타겟", info.buildTarget.ToString());
                EditorCommon.DrawLabelTextField("경로", info.outputPath);
                EditorCommon.DrawLabelTextField("시작", info.startTime.ToString(CultureInfo.CurrentCulture));
                EditorCommon.DrawLabelTextField("종료", info.endTime.ToString(CultureInfo.CurrentCulture));
                EditorCommon.DrawLabelTextField("시간", info.buildTime.ToString());

                GUILayout.Space(5f);
                
                _buildInfoMemoScrollViewPosition = GUILayout.BeginScrollView(_buildInfoMemoScrollViewPosition, false, false, GUILayout.Height(200f));
                EditorGUILayout.TextArea(info.memo, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                GUILayout.EndScrollView();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawBuildResultRecordCursorNavigator() {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button("<", GUILayout.Height(40f))) {
                config.Cursor = config.Cursor <= 0 ? config.GetRecordCount() : config.Cursor - 1;
            }

            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(200f))) {
                GUILayout.Label($"빌드 기록 [{config.Cursor + 1} / {config.GetRecordCount()}]", Constants.Draw.TITLE_STYLE);
                using (new GUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();
                    if (EditorCommon.DrawFitButton("빌드 기록 제거")) {
                        Logger.TraceLog($"Deleted the build result log at index {config.Cursor}", Color.red);
                        config.DeleteBuildRecord(config.Cursor);
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            if (GUILayout.Button(">", GUILayout.Height(40f))) {
                config.Cursor = (config.Cursor + 1) % config.GetRecordCount();
            }
        }
    }

    private void RefreshScenes() {
        activateSceneDic.Clear();
        if (EditorBuildSettings.scenes.Any()) {
            activateSceneDic = EditorBuildSettings.scenes.ToDictionary(scene => scene.path, scene => scene);
        }

        sceneAssetInfoDic.Clear();
        foreach (var info in AssetDatabaseUtil.FindAssetInfos<SceneAsset>(FilterUtil.CreateFilter(TypeFilter.Scene))) {
            sceneAssetInfoDic.Add(info.path, info);
        }
    }
    
    protected virtual void Build(string memo = "") {
        BuildConfigProvider.Load(config);
        if (BuildInteractionInterface.TryAttachBuilder(builderType, out var builder)) {
            var report = builder.StartBuild();
            if (report != null && config.isLogBuildReport) {
                if (Service.TryGetServiceWithRestart<LogCollectorService>(out var service)) {
                    service.ClearLog();
                    service.SetFilter(LogType.Error);
                }

                config.AddBuildRecord(new BuildRecord {
                    result = report.summary.result,
                    buildTarget = report.summary.platform,
                    outputPath = report.summary.outputPath,
                    startTime = report.summary.buildStartedAt,
                    endTime = report.summary.buildEndedAt,
                    buildTime = report.summary.totalTime,
                    memo = $"{memo}\n\n===================\n\n" +
                           $"{config.ToStringAllFields()}\n\n===================\n\n" +
                           $"{service?.Copy().ToStringCollection('\n')}",
                });
                
                config.Save(CONFIG_PATH);
                config.ResetCursor();

                Service.StopService<LogCollectorService>();
            }
        }
    }
    
    protected virtual void SwitchPlatform() {
        if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        }
    }
}

public class EditorBuildDrawerAttribute : Attribute {

    public readonly Type builderType;
    public readonly Enum buildType;

    public EditorBuildDrawerAttribute(Type builderType) {
        if (builderType.IsSubclassOf(typeof(BuilderBase))) {
            this.builderType = builderType;
            if (builderType.TryGetCustomAttribute<BuilderAttribute>(out var attribute)) {
                buildType = attribute.buildType;
            }
        } else {
            Logger.TraceError($"{builderType.Name} is Invalid {nameof(builderType)}. {nameof(builderType)} must inherit from {nameof(BuilderBase)}.");
        }
    }

    public EditorBuildDrawerAttribute(object buildType) {
        if (buildType is Enum enumValue) {
            this.buildType = enumValue;
            foreach (var type in ReflectionProvider.GetSubTypesOfType<BuilderBase>()) {
                if (type.TryGetCustomAttribute<BuilderAttribute>(out var attribute) && attribute.buildType.Equals(this.buildType)) {
                    builderType = type;
                    return;
                }
            }
            
            Logger.TraceError($"{nameof(enumValue)} is invalid || {enumValue}. Missing target {nameof(BuilderBase)}");
        }
    }
}