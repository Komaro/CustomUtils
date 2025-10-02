using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Provider, RESOURCE_TYPE.Addressable)]
public class EditorAddressableProviderDrawer : EditorResourceDrawer<AddressableProviderConfig, AddressableProviderConfig.NullConfig> {

    private AddressableAssetSettings _settings;

    private string[] _profiles;
    private int _selectProfile;
    private AddressableAssetProfileSettings.ProfileIdData _selectProfileData;
    
    protected override string CONFIG_NAME => $"{nameof(EditorAddressableProviderDrawer)}{Constants.Extension.JSON}";
    
    public EditorAddressableProviderDrawer(EditorWindow window) : base(window) { }

    public override void CacheRefresh() {
        base.CacheRefresh();

        _settings ??= AddressableAssetSettingsDefaultObject.Settings;

        if (_settings != null) {
            _profiles = _settings.profileSettings.GetAllProfileNames().ToArray();
        }
    }

    public override void Draw() {
        base.Draw();

        EditorCommon.DrawSeparator();
        
        if (_settings == null) {
            EditorGUILayout.HelpBox($"{nameof(AddressableAssetSettings)}를 찾을 수 없습니다.", MessageType.Error);
            return;
        }

        
        DrawProfile();
        EditorCommon.DrawSeparator();
        
        DrawNext();
        EditorCommon.DrawSeparator();
    }

    private void DrawProfile() {
        using (var scope = new EditorGUI.ChangeCheckScope()) {
            _selectProfile = EditorGUILayout.Popup(_selectProfile, _profiles);
            if (scope.changed) {
                var id = _settings.profileSettings.GetProfileId(_profiles[_selectProfile]);
                if (string.IsNullOrEmpty(id)) {
                    Logger.TraceError($"{nameof(id)} is null or empty");
                    return;
                }
                
                _settings.activeProfileId = id;
            }
        }

        if (_selectProfileData != null) {
            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawLabelTextField(nameof(_selectProfileData.Id), _selectProfileData.Id);
                EditorCommon.DrawLabelTextField(nameof(_selectProfileData.ProfileName), _selectProfileData.ProfileName);
            }
        }
    }

    private void DrawNext() {
        using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
            EditorCommon.DrawLabelTextField(nameof(_settings.RemoteCatalogBuildPath), _settings.RemoteCatalogBuildPath.GetValue(_settings), 200f);
            EditorCommon.DrawLabelTextField(nameof(_settings.RemoteCatalogLoadPath), _settings.RemoteCatalogLoadPath.GetValue(_settings), 200f);

            if (GUILayout.Button("Test")) {
                foreach (var locator in Addressables.ResourceLocators) {
                    Logger.TraceLog(locator.Keys.ToStringCollection(x => x.ToString()));
                }
            }
        }
    }
}

public class AddressableProviderConfig : JsonCoroutineAutoConfig {

    public override bool IsNull() => this is NullConfig;
    
    public class NullConfig : AddressableProviderConfig { }
}