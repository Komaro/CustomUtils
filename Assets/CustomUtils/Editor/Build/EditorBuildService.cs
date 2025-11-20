using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class EditorBuildService : EditorService<EditorBuildService> {

    private int _selectBuilderIndex;
    private Type _selectBuilderType;
    
    private Type[] _builderTypes;
    private string[] _builderTypeNames;

    private readonly Dictionary<Type, EditorDrawer> _drawerDic = new();
    
    private readonly string SELECT_DRAWER_KEY = $"{nameof(EditorBuildService)}_{nameof(EditorDrawer)}";

    [MenuItem("Service/Build/Build Service")]
    public static void OpenWindow() => Window.Show();

    protected override void Refresh() {
        if (HasOpenInstances<EditorBuildService>()) {
            _builderTypes = ReflectionProvider.GetSubTypesOfType<BuilderBase>().OrderBy(type => type.TryGetCustomAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999).ToArray();
            if (_builderTypes.Any()) {
                _builderTypeNames = _builderTypes.Select(type => type.TryGetCustomAttribute<AliasAttribute>(out var attribute) ? attribute.alias : type.Name).ToArray();
            }
            
            foreach (var type in ReflectionProvider.GetSubTypesOfTypeDefinition(typeof(EditorBuildDrawer<,>))) {
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
        if (_builderTypes is not { Length: > 0 }) {
            EditorGUILayout.HelpBox($"{nameof(BuilderBase)}를 상속받은 구현이 존재하지 않습니다.", MessageType.Error);
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
        
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            drawer?.Draw();
            EditorCommon.DrawSeparator();
        } else {
            EditorGUILayout.HelpBox($"유효한 {typeof(EditorBuildDrawer<,>).Name}를 찾을 수 없습니다.", MessageType.Warning);
        }
    }
    
    private void DrawerCacheRefresh() {
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            drawer?.CacheRefresh();
        }
    }

    private void DrawerClose() {
        if (_drawerDic.TryGetValue(_selectBuilderType, out var drawer)) {
            drawer?.Close();
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

public struct BuildRecord {

    public BuildResult result;
    public BuildTarget buildTarget;
    public string outputPath;
    public DateTime startTime;
    public DateTime endTime;
    public TimeSpan buildTime;
    public string memo;
}

[RequiresAttributeImplementation(typeof(BuildConfigAttribute))]
public abstract class BuildConfig : JsonCoroutineAutoConfig {

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

    public bool isLogBuildReport;

    #region [Build Record]
    
    private const int MAX_LOG_COUNT = 5;
    
    [JsonProperty("lastBuildInfoList")]
    private readonly List<BuildRecord> _lastBuildRecordList = new();
    
    [JsonIgnore] public BuildRecord this[int index] => _lastBuildRecordList[index];
    
    [JsonIgnore] private int _cursor;
    [JsonIgnore] public int Cursor { get => _cursor; set => _cursor = Math.Clamp(value, 0, _lastBuildRecordList.Count - 1); }
    
    public void AddBuildRecord(BuildRecord record) => _lastBuildRecordList.LimitedAdd(record, MAX_LOG_COUNT);

    public void DeleteBuildRecord(int index) {
        if (_lastBuildRecordList.IsValidIndex(index) == false) {
            return;
        }
        
        _lastBuildRecordList.RemoveAt(index);
        _cursor = _lastBuildRecordList.Count > 0 ? Math.Clamp(_cursor, 0, _lastBuildRecordList.Count - 1) : 0;
    }
    
    public int GetRecordCount() => _lastBuildRecordList.Count;
    
    public void ResetCursor() => Cursor = MAX_LOG_COUNT;
    
    #endregion
    
    public BuildConfig() {
        if (GetType().TryGetCustomAttribute<BuildConfigAttribute>(out var targetAttribute)) {
            defineSymbols = targetAttribute.buildTargetGroup.GetScriptingDefineSymbolsForGroup();
            foreach (var (optionAttribute, enumType) in ReflectionProvider.GetAttributeEnumSets<BuildOptionEnumAttribute>()) {
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
        
        foreach (var logType in EnumUtil.AsSpan<LogType>()) {
            stackTraceDic.AutoAdd(logType, PlayerSettings.GetStackTraceLogType(logType));
        }
        
        developmentBuild = EditorUserBuildSettings.development;
        autoConnectProfile = EditorUserBuildSettings.connectProfiler;
        deepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;
        scriptDebugging = EditorUserBuildSettings.allowDebugging;
    }
}

[BuildOptionEnum]
public enum DEFAULT_CUSTOM_BUILD_OPTION {
    cleanBurstDebug,
    cleanIL2CPPSludge,
    revealInFinder,
    
    // TODO. 예외 작업
    ignoreResourcesReimport,
    refreshAssetDatabase,
}