using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public partial class EditorBuildService {

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawDrawer() {
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            _editorWindowScrollViewPosition = EditorGUILayout.BeginScrollView(_editorWindowScrollViewPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            drawer?.Draw();
            EditorGUILayout.EndScrollView();
            
            EditorCommon.DrawSeparator();
        } else {
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorBuildDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }
    
    private static void DrawerCacheRefresh() {
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            drawer?.CacheRefresh();
        }
    }

    private static void DrawerClose() {
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            drawer?.Close();
        }
    }
}

public class EditorBuildDrawerAttribute : Attribute {

    public readonly Type builderType;
    public readonly Enum buildType;

    public EditorBuildDrawerAttribute(Type builderType) {
        if (builderType.IsSubclassOf(typeof(Builder))) {
            this.builderType = builderType;
            if (builderType.TryGetCustomAttribute<BuilderAttribute>(out var attribute)) {
                buildType = attribute.buildType;
            }
        } else {
            Logger.TraceError($"{builderType.Name} is Invalid {nameof(builderType)}. {nameof(builderType)} must inherit from {nameof(Builder)}.");
        }
    }

    public EditorBuildDrawerAttribute(object buildType) {
        if (buildType is Enum enumValue) {
            this.buildType = enumValue;
            foreach (var type in ReflectionProvider.GetSubClassTypes<Builder>()) {
                if (type.TryGetCustomAttribute<BuilderAttribute>(out var attribute) && attribute.buildType.Equals(this.buildType)) {
                    builderType = type;
                    return;
                }
            }
            
            Logger.TraceError($"{nameof(enumValue)} is invalid || {enumValue}. Missing target {nameof(Builder)}");
        }
    }
}

[RequiresAttributeImplementation(typeof(EditorBuildDrawerAttribute))]
public abstract class EditorBuildDrawer<TConfig, TNullConfig> : EditorAutoConfigDrawer<TConfig, TNullConfig> 
    where TConfig : BuildConfig, new() 
    where TNullConfig : TConfig, new() {
    
    protected readonly Type builderType;
    protected readonly BuildTarget buildTarget;
    protected readonly BuildTargetGroup buildTargetGroup;
    
    protected ToggleDraw[] defineSymbols = {};
    
    private Vector2 _defineSymbolScrollViewPosition;
    private bool _activateSceneFoldOut;
    private bool _sceneAssetFoldOut;
    
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
                return type.TryGetField(name, out var info) && info.TryGetCustomAttribute<EnumValueAttribute>(out var attribute)
                    ? new ToggleDraw(name, configDefineSymbolSet.Contains(name), attribute.header)
                    : new ToggleDraw(name, configDefineSymbolSet.Contains(name));
            })).ToArray();
        }
        
        buildOptionSet = ReflectionProvider.GetAttributeEnumInfos<BuildOptionEnumAttribute>()
            .Where(info => info.attribute.buildTargetGroup == BuildTargetGroup.Unknown || info.attribute.buildTargetGroup == buildTargetGroup)
            .ConvertTo(info => Enum.GetValues(info.enumType).Cast<object>()).Select(ob => ob.ToString()).ToHashSetWithDistinct();
        
        RefreshScenes();
    }

    private static void RefreshScenes() {
        activateSceneDic.Clear();
        if (EditorBuildSettings.scenes.Any()) {
            activateSceneDic = EditorBuildSettings.scenes.ToDictionary(scene => scene.path, scene => scene);
            Logger.TraceLog(activateSceneDic.ToStringCollection(x => x.Key));
        }

        sceneAssetInfoDic.Clear();
        foreach (var info in AssetDatabaseUtil.FindAssetInfos<SceneAsset>("t:Scene")) {
            Logger.TraceLog($"{info.Name} || {info.guid} || {info.path}");
            sceneAssetInfoDic.Add(info.path, info);
        }
    }

    public override void Draw() {
        base.Draw();
        if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
            EditorGUILayout.HelpBox($"현재 활성화된 {nameof(BuildTarget)}({EditorUserBuildSettings.activeBuildTarget})과 {nameof(Builder)}의 {nameof(BuildTarget)}({buildTarget})이 일치하지 않습니다", MessageType.Warning);
            if (GUILayout.Button($"{nameof(BuildTarget)} 전환\n[{EditorUserBuildSettings.activeBuildTarget} ==> {buildTarget}]")) {
                SwitchPlatform();
            }
        }

        if (config.IsNull() == false) {
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
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void DrawStackTrace() {
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            GUILayout.Label("스택 트레이스 (Stack Trace)", Constants.Draw.BOLD_CENTER_LABEL);
            using (new EditorGUILayout.HorizontalScope()) {
                foreach (var traceType in EnumUtil.GetValues<StackTraceLogType>()) {
                    if (GUILayout.Button(traceType.ToString())) {
                        foreach (var logType in EnumUtil.GetValues<LogType>()) {
                            config.stackTraceDic[logType] = traceType;
                        }
                    }
                }
            }
                
            foreach (var logType in EnumUtil.GetValues<LogType>()) {
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
                config.defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                
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
    protected void DrawBuildButton() {
        EditorCommon.DrawSeparator();
        
        GUILayout.Label("빌드 (Build)", Constants.Draw.AREA_TITLE_STYLE);
        using (new EditorGUILayout.VerticalScope()) {
            EditorCommon.DrawLabelTextField("빌더 빌드 타겟", buildTarget.ToString());
            EditorCommon.DrawLabelTextField("현재 빌드 타겟", EditorUserBuildSettings.activeBuildTarget.ToString());
            
            if (GUILayout.Button("Build", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.Height(45f))) {
                if (EditorBuildSettings.scenes.Any(scene => scene.enabled) == false) {
                    EditorUtility.DisplayDialog("경고", $"{nameof(EditorBuildSettings)}에 활성화 된 씬이 존재하지 않습니다.", "확인");
                    return;
                }
            
                if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
                    EditorCommon.ShowCheckDialogue($"{nameof(buildTarget)} miss match", $"현재 선택된 {nameof(BuildTarget)}({buildTarget})과 활성화된 {nameof(BuildTarget)}({EditorUserBuildSettings.activeBuildTarget})이 동일하지 않습니다. 플랫폼 전환 후 빌드를 진행합니다", ok: Build);
                } else {
                    Build();
                }
            }
        }
    }

    protected void SwitchPlatform() {
        if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        }
    }

    protected void Build() {
        BuildConfigProvider.Load(config);
        if (BuildInteractionInterface.TryAttachBuilder(builderType, out var builder)) {
            var report = builder.StartBuild();
            if (report != null) {
                Logger.SimpleTraceLog($"Memo\n{report.summary.ToStringAllFields()}");
            }
        }
    }
}

/// <param name="buildType">Enum Value</param>
/// <param name="buildTarget"></param>
/// <param name="buildTargetGroup"></param>
public class BuildConfigAttribute : PriorityAttribute {

    public Enum buildType;
    public BuildTarget buildTarget;
    public BuildTargetGroup buildTargetGroup;

    public BuildConfigAttribute() {
        buildTarget = BuildTarget.NoTarget;
        buildTargetGroup = BuildTargetGroup.Unknown;
    }
    
    public BuildConfigAttribute(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) {
        this.buildTarget = buildTarget;
        this.buildTargetGroup = buildTargetGroup;
    }

    public BuildConfigAttribute(object buildType, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) : this(buildTarget, buildTargetGroup) {
        if (buildType is Enum enumValue) {
            this.buildType = enumValue;
        }
    }
}

[RequiresAttributeImplementation(typeof(BuildConfigAttribute))]
public abstract class BuildConfig : JsonAutoConfig {

    public string buildDirectory = string.Empty;

    public readonly Dictionary<string, bool> optionDic = new();

    public string defineSymbols;
    public string applicationIdentifier;
    public string bundleVersion;
    public readonly Dictionary<LogType, StackTraceLogType> stackTraceDic = new(); 

    public bool developmentBuild;
    public bool autoConnectProfile;
    public bool deepProfilingSupport;
    public bool scriptDebugging;

    public BuildConfig() {
        var type = GetType();
        if (type.TryGetCustomAttribute<BuildConfigAttribute>(out var targetAttribute)) {
            defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetAttribute.buildTargetGroup);
            foreach (var (optionAttribute, enumType) in ReflectionProvider.GetAttributeEnumInfos<BuildOptionEnumAttribute>()) {
                if (optionAttribute.buildTargetGroup == BuildTargetGroup.Unknown || optionAttribute.buildTargetGroup == targetAttribute.buildTargetGroup) {
                    foreach (var ob in Enum.GetValues(enumType)) {
                        optionDic.AutoAdd(ob.ToString(), false);
                    }
                }
            }
        } else {
            defineSymbols = string.Empty;
            optionDic.Clear();
        }
        
        applicationIdentifier = PlayerSettings.applicationIdentifier;
        bundleVersion = PlayerSettings.bundleVersion;
        
        foreach (var logType in EnumUtil.GetValues<LogType>()) {
            stackTraceDic.AutoAdd(logType, PlayerSettings.GetStackTraceLogType(logType));
        }
        
        developmentBuild = EditorUserBuildSettings.development;
        autoConnectProfile = EditorUserBuildSettings.connectProfiler;
        deepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;
        scriptDebugging = EditorUserBuildSettings.allowDebugging;
    }

    public bool IsActiveOption<TEnum>(TEnum option) where TEnum : struct, Enum => optionDic.TryGetValue(option.ToString(), out var isActive) && isActive;
    public bool IsActiveOption(string option) => optionDic.TryGetValue(option, out var isActive) && isActive;
}

[BuildConfig(BuildTarget.NoTarget, BuildTargetGroup.Unknown)]
public class NullBuildConfig : BuildConfig {

    public override bool IsNull() => true;
}

[BuildOptionEnum]
public enum DEFAULT_CUSTOM_BUILD_OPTION {
    ignoreResourcesReimport,
    cleanBurstDebug,
    cleanIL2CPPSludge,
    revealInFinder,
    refreshAssetDatabase,
}