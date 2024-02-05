using System;
using System.Collections.Generic;
using System.Linq;

public class UIAttribute : Attribute {
    public readonly string prefabs;
    public readonly AnchorPresets anchorType;
    public readonly UIGroupType[] groups;

    public UIAttribute(string prefabs) {
        this.prefabs = prefabs;
        this.anchorType = default;
        this.groups = new UIGroupType[] { };
    }

    public UIAttribute(string prefabs, AnchorPresets anchorType) {
        this.prefabs = prefabs;
        this.anchorType = anchorType;
        this.groups = new UIGroupType[] { };
    }
	
    public UIAttribute(string prefabs, params UIGroupType[] groups) {
        this.prefabs = prefabs;
        this.anchorType = default;
        this.groups = groups.Length <= 0 ? new UIGroupType[] { } : groups;
    }
    
	
    public UIAttribute(string prefabs, AnchorPresets anchorType, params UIGroupType[] groups) {
        this.prefabs = prefabs;
        this.anchorType = anchorType;
        this.groups = groups.Length <= 0 ? new UIGroupType[] { } : groups;
    }

    public void Deconstruct(out string prefabs, out AnchorPresets anchorType, out List<UIGroupType> groups) {
        prefabs = this.prefabs;
        anchorType = this.anchorType;
        groups = this.groups.ToList();
    }
}