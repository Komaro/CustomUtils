using System;
using UnityEditor;

[AttributeUsage(AttributeTargets.Enum)]
public class BuildOptionEnumAttribute : Attribute {
    
    public BuildTargetGroup buildTargetGroup;

    public BuildOptionEnumAttribute() => buildTargetGroup = BuildTargetGroup.Unknown;
    public BuildOptionEnumAttribute(BuildTargetGroup buildTargetGroup) => this.buildTargetGroup = buildTargetGroup;
}