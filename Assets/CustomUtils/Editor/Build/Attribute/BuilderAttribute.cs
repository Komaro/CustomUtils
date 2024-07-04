using System;
using UnityEditor;

[AttributeUsage(AttributeTargets.Class)]
public class BuilderAttribute : Attribute {
    
    public Enum buildType;
    public BuildTarget buildTarget;
    public BuildTargetGroup buildTargetGroup;
    
    /// <param name="buildType">enum Value</param>
    /// <param name="buildTarget"></param>
    /// <param name="buildTargetGroup"></param>
    public BuilderAttribute(object buildType, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) {
        this.buildType = buildType is Enum enumType ? enumType : default;
        this.buildTarget = buildTarget;
        this.buildTargetGroup = buildTargetGroup;
    }
}