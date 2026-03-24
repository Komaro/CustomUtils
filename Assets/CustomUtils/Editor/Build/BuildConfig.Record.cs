using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;

public partial class BuildConfig {
    
    [JsonProperty("lastBuildInfoList")]
    private readonly List<BuildRecord> _lastBuildRecordList = new();
    
    [JsonIgnore] 
    private int _cursor;
    
    [JsonIgnore] 
    public int Cursor { get => _cursor; set => _cursor = Math.Clamp(value, 0, _lastBuildRecordList.Count - 1); }
    
    private const int MAX_LOG_COUNT = 5;
    
    [JsonIgnore] 
    public BuildRecord this[int index] => _lastBuildRecordList[index];
    
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
}

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

    /// <summary>
    /// <param name="buildType">Only <see cref="Enum"/> types are supported type</param>
    /// </summary>
    public BuildConfigAttribute(object buildType, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) : this(buildTarget, buildTargetGroup) {
        if (buildType is Enum enumValue) {
            this.buildType = enumValue;
        }
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