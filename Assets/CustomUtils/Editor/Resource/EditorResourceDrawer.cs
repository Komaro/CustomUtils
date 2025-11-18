using System;
using UnityEditor;

[AttributeUsage(AttributeTargets.Class)]
public class EditorResourceDrawerAttribute : Attribute {

    public readonly Enum menuType;
    public readonly Enum resourceType;
    
    public EditorResourceDrawerAttribute(object menuType, object resourceType) {
        if (menuType is Enum menuValue && resourceType is Enum serviceValue) {
            this.menuType = menuValue;
            this.resourceType = serviceValue;
        }
    }
}

[RequiresAttributeImplementation(typeof(EditorResourceDrawerAttribute))]
public abstract class EditorResourceDrawer<TConfig, TNullConfig> : EditorAutoConfigDrawer<TConfig, TNullConfig>
    where TConfig : JsonAutoConfig, new() 
    where TNullConfig : TConfig, new() {

    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_PATH}/{nameof(EditorResourceService)}/{CONFIG_NAME}";

    protected EditorResourceDrawer(EditorWindow window) : base(window) { }
}