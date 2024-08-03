using UnityEditor;

public static partial class EditorSettingsService {
    
    private static bool _isChangePipelineAutoRefresh;
    private static ASSET_PIPELINE_AUTO_REFRESH_TYPE _activeRefreshType;

    private const string ASSET_PIPELINE_AUTO_REFRESH_DISABLE = "Service/EditorSettings/Asset Pipeline/Active Auto Refresh Disable";
    private const string ASSET_PIPELINE_AUTO_REFRESH_ENABLE = "Service/EditorSettings/Asset Pipeline/Active Auto Refresh Enable";
    private const string ASSET_PIPELINE_AUTO_REFRESH_ENABLED_OUTSIDE_PLAYMODE = "Service/EditorSettings/Asset Pipeline/Active Auto Refresh EnabledOutsidePlayMode";
    
    static EditorSettingsService() => EditorPrefsUtil.TryGet(EditorPrefsUtil.ASSET_PIPELINE_AUTO_REFRESH, out _activeRefreshType);

    [MenuItem(ASSET_PIPELINE_AUTO_REFRESH_DISABLE, false, 10000)]
    private static void ActiveAssetPipelineAutoRefresh_Disable() {
        if (_activeRefreshType != ASSET_PIPELINE_AUTO_REFRESH_TYPE.DISABLE) {
            _activeRefreshType = ASSET_PIPELINE_AUTO_REFRESH_TYPE.DISABLE;
            EditorPrefsUtil.Set(EditorPrefsUtil.ASSET_PIPELINE_AUTO_REFRESH, _activeRefreshType);
        }
    }
    
    [MenuItem(ASSET_PIPELINE_AUTO_REFRESH_ENABLE, false, 10001)]
    private static void ActiveAssetPipelineAutoRefresh_Enable() {
        if (_activeRefreshType != ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLE) {
            _activeRefreshType = ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLE;
            EditorPrefsUtil.Set(EditorPrefsUtil.ASSET_PIPELINE_AUTO_REFRESH, _activeRefreshType);
        }
    }
    
    [MenuItem(ASSET_PIPELINE_AUTO_REFRESH_ENABLED_OUTSIDE_PLAYMODE, false, 10002)]
    private static void ActiveAssetPipelineAutoRefresh_EnabledOutsidePlayMode() {
        if (_activeRefreshType != ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLED_OUTSIDE_PLAYMODE) {
            _activeRefreshType = ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLED_OUTSIDE_PLAYMODE;
            EditorPrefsUtil.Set(EditorPrefsUtil.ASSET_PIPELINE_AUTO_REFRESH, _activeRefreshType);
        }
    }
    
    [MenuItem(ASSET_PIPELINE_AUTO_REFRESH_DISABLE, true)]
    private static bool ActiveAssetPipelineAutoRefreshValidation() {
        Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_DISABLE, false);
        Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_ENABLE, false);
        Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_ENABLED_OUTSIDE_PLAYMODE, false);
        
        switch (_activeRefreshType) {
            case ASSET_PIPELINE_AUTO_REFRESH_TYPE.DISABLE:
                Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_DISABLE, true);
                break;
            case ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLE:
                Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_ENABLE, true);
                break;
            case ASSET_PIPELINE_AUTO_REFRESH_TYPE.ENABLED_OUTSIDE_PLAYMODE:
                Menu.SetChecked(ASSET_PIPELINE_AUTO_REFRESH_ENABLED_OUTSIDE_PLAYMODE, true);
                break;
        }
        
        return true;
    }

    private enum ASSET_PIPELINE_AUTO_REFRESH_TYPE {
        DISABLE = 0,
        ENABLE = 1,
        ENABLED_OUTSIDE_PLAYMODE = 2,
    }
}