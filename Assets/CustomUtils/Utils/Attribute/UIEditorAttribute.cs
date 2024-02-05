using System;
using UnityEngine;

/// <summary>
/// Need Only UnityEditor Custom Inspector Draw.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class BoolCheckFoldAttribute : PropertyAttribute {

	public readonly string targetFieldName;

	public BoolCheckFoldAttribute(string targetFieldName) => this.targetFieldName = targetFieldName;
}

[AttributeUsage(AttributeTargets.Field)]
public class UIConfigBoolCheckFoldAttribute : BoolCheckFoldAttribute {
    
    public UIConfigBoolCheckFoldAttribute(string targetFieldName) : base(targetFieldName) { }
}
