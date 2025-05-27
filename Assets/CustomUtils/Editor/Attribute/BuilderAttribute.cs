using System;
using UnityEditor;

[AttributeUsage(AttributeTargets.Class)]
public class BuilderAttribute : Attribute {
    
    public Enum buildType;
    public BuildTarget buildTarget;
    public BuildTargetGroup buildTargetGroup;
    
    /// <param name="buildType">Enum Value</param>
    /// <param name="buildTarget"></param>
    /// <param name="buildTargetGroup"></param>
    public BuilderAttribute(object buildType, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) {
        this.buildType = buildType as Enum;
        this.buildTarget = buildTarget;
        this.buildTargetGroup = buildTargetGroup;
    }
}

public enum BuilderTestEnum {
    NONE,
    FIRST,
}

[Builder(BuilderTestEnum.FIRST, BuildTarget.Android, BuildTargetGroup.Android)]
public class BuilderTest {
    
}
