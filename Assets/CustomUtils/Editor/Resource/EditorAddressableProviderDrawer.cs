using UnityEditor;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Provider, RESOURCE_SERVICE_TYPE.Addressable)]
public class EditorAddressableProviderDrawer : EditorResourceDrawer<AddressableProviderConfig, AddressableProviderConfig.NullConfig> {
    
    protected override string CONFIG_NAME => nameof(EditorAddressableProviderDrawer);
    
    public EditorAddressableProviderDrawer(EditorWindow window) : base(window) {
    }
}

public class AddressableProviderConfig : JsonAutoConfig {

    public override bool IsNull() => this is NullConfig;
    public class NullConfig : AddressableProviderConfig { }
}