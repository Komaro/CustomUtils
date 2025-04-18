using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;
#if  UNITY_IOS
using UnityEditor.iOS.Xcode;
// using AppleAuth.Editor;
#endif

[Builder(SAMPLE_BUILD_TYPE.IOS, BuildTarget.iOS, BuildTargetGroup.iOS)]
public class Sample_Builder_Ios : Builder {

    // Ex) "Assets/Plugins/..."
    private List<string> _removeDirectoryList = new ();

    protected const string DEFAULT_APPLICATION_IDENTIFIER = "SOME_DEFAULT_APPLICATION_IDENTIFIER";
    protected const string DEFAULT_DEVELOPER_TEAM_ID = "SOME_DEFAULT_DEVELOPER_TEAM_ID";
    
    protected const string DEFAULT_DEVELOPMENT_PROFILE = "SOME_DEFAULT_DEVELOPMENT_PROFILE";
    protected const string DEFAULT_DISTRIBUTE_PROFILE = "SOME_DEFAULT_DISTRIBUTE_PROFILE";

    protected const string DEFAULT_TARGET_OS_VERSION = "13.0";
    
    protected Regex POST_INSTALL_REGEX = new (@"post_install\s*?do", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    protected Regex DUPLICATE_CHECK_REGEX = new (@"\'CODE_SIGNING_ALLOWED\'|\'CODE_SIGNING_REQUIRED\'|\'EXPANDED_CODE_SIGN_IDENTITY\'");
    protected Regex INSTALLER_REGEX = new (@"\|installer\|", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    protected string EXECUTE_PATH => $"{Constants.Path.PROJECT_PATH}/BuildExecute/{GetType()?.GetCustomAttribute<BuilderAttribute>()?.buildType.ToString()}";
    
    protected const string POD_FILE = "Podfile";
    protected const string INSTALL_POD_SHELL = "SOME_INSTALL_POD_SHELL";

    protected const string APPEND_POD_CONTENT_PREFIX = @"post_install do |installer|";
    protected const string APPEND_POD_CONTENT = @"
    installer.generated_projects.each do |target|
      target.build_configurations.each do |config|
        config.build_settings['CODE_SIGNING_ALLOWED'] = 'NO'
        config.build_settings['CODE_SIGNING_REQUIRED'] = 'NO'
        config.build_settings['EXPANDED_CODE_SIGN_IDENTITY'] = ''
        end
    end";
    protected const string APPEND_POD_CONTENT_SUFFIX = "\nend";

    public override BuildReport StartBuild(BuildPlayerOptions buildOptions) {
        SetDevelopmentBuild(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.developmentBuild)));
        SetAutoConnectProfile(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.autoConnectProfile)));
        SetDeepProfilingSupport(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.deepProfilingSupport)));
        SetScriptDebugging(BuildConfigProvider.GetValue<bool>(nameof(BuildConfig.scriptDebugging)));
        
        // Set Build Player Option Development Options
        SetConditionBuildOptions(ref buildOptions, EditorUserBuildSettings.development, BuildOptions.Development);
        SetConditionBuildOptions(ref buildOptions, EditorUserBuildSettings.connectProfiler, BuildOptions.ConnectWithProfiler);
        SetConditionBuildOptions(ref buildOptions, EditorUserBuildSettings.buildWithDeepProfilingSupport, BuildOptions.EnableDeepProfilingSupport);
        SetConditionBuildOptions(ref buildOptions, EditorUserBuildSettings.allowDebugging, BuildOptions.AllowDebugging);

        return base.StartBuild(buildOptions);
    }

    protected override void OnPreProcess(ref BuildPlayerOptions options) {
#if UNITY_IOS
        // Set Target OS Version
        SetTargetOSVersion(DEFAULT_TARGET_OS_VERSION);
        
        // Set Symbols
        SetDefineSymbols(BuildTargetGroup.iOS, BuildSettings.Instance.GetValue<string>("defineSymbols"));
 
        // Set Common Option
        SetApplicationIdentifier(BuildTargetGroup.iOS, DEFAULT_APPLICATION_IDENTIFIER);
        SetVersionName(BuildConfigProvider.GetValue<string>(nameof(SampleIosBuildConfig.bundleVersion)));
        
        // Set iOS Build Settings
        SetBuildNumber(BuildConfigProvider.GetValue<string>(nameof(SampleIosBuildConfig.buildNumber)));
        SetAppleEnableAutomaticSigning(false);
        SetAppleDeveloperTeamId(BuildConfigProvider.GetValue<string>(nameof(SampleIosBuildConfig.appleDeveloperTeamID)));
        if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID)) {
            SetAppleDeveloperTeamId(DEFAULT_DEVELOPER_TEAM_ID);
        }
        
        SetProvisioningProfileType(BuildConfigProvider.GetValue<ProvisioningProfileType>(nameof(SampleIosBuildConfig.iOSManualProvisioningProfileType)));
        
        if (string.IsNullOrEmpty(BuildConfigProvider.GetValue<string>(nameof(SampleIosBuildConfig.iOSManualProvisioningProfileID)))) {
            switch (PlayerSettings.iOS.iOSManualProvisioningProfileType) {
                case ProvisioningProfileType.Development:
                    SetiOSManualProvisioningProfileID(DEFAULT_DEVELOPMENT_PROFILE);
                    break;
                case ProvisioningProfileType.Distribution:
                    SetiOSManualProvisioningProfileID(DEFAULT_DISTRIBUTE_PROFILE);
                    break;
            }
        } else {
            SetiOSManualProvisioningProfileID(BuildConfigProvider.GetValue<string>(nameof(SampleIosBuildConfig.iOSManualProvisioningProfileID)));
        }
        
        SetHideHomeButton(true);
        
        // Delete Old Export Project
        SystemUtil.DeleteDirectory(options.locationPathName);
        
        // Delete Invalid Folder
        _removeDirectoryList.ForEach(SystemUtil.DeleteDirectory);
#endif
    }

    protected override void OnPostProcess(BuildSummary summary) {
#if UNITY_IOS
        try {
            #region [Pod]

            try {
                FixPodfile(summary.outputPath);
                SystemUtil.ExecuteScript($"{EXECUTE_PATH}/{INSTALL_POD_SHELL}", EXECUTE_PATH, $"{summary.outputPath}");
            } catch (Exception e) {
                Debug.LogError(e);
                throw;
            }
            
            #endregion
        
            var projectPath = PBXProject.GetPBXProjectPath(summary.outputPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
            var targetGUID = pbxProject.GetUnityMainTargetGuid();
            var unityFrameworkGUID = pbxProject.GetUnityFrameworkTargetGuid();
#else
            var targetGUID = pbxProject.TargetGuidByName(PBXProject.GetUnityTargetName());
            var unityFrameworkGUID = pbxProject.TargetGuidByName("UnityFramework");
#endif
            
            #region [Framework]
            
            pbxProject.AddFrameworkToProject(targetGUID, "UnityFramework.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "GameKit.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "iAd.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "AdSupport.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "CoreTelephony.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "StoreKit.framework", false);
            pbxProject.AddFrameworkToProject(targetGUID, "UserNotifications.framework", false);
            
            pbxProject.AddFrameworkToProject(unityFrameworkGUID, "AppTrackingTransparency.framework", false);
            pbxProject.AddFrameworkToProject(unityFrameworkGUID, "GameKit.framework", false);
            
            #endregion
            
            #region [Build Property]
            
            pbxProject.SetBuildProperty(targetGUID, "ENABLE_BITCODE", "false");
            pbxProject.SetBuildProperty(targetGUID, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            
            pbxProject.AddBuildProperty(targetGUID, "LD_RUNPATH_SEARCH_PATHS", "/usr/lib/swift");
            pbxProject.AddBuildProperty(targetGUID, "LD_RUNPATH_SEARCH_PATHS", "libswiftCore.dylib");
            
            pbxProject.SetBuildProperty(unityFrameworkGUID, "ENABLE_BITCODE", "false");
            pbxProject.SetBuildProperty(unityFrameworkGUID, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");   
            
            #endregion
            
            #region [Add Resource]
            
            #endregion

            #region [plist]
            
            var plistPath = Path.Combine(summary.outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            
            var plistElementDict = plist.root;
            
            // String plist
            plistElementDict.SetBoolean("ITSAppUsesNonExemptEncryption", false); 
            plistElementDict.SetString("NSUserTrackingUsageDescription", "Your data will be used to deliver a personalized experience and to improve our products and services."); 
            
            // Array plist
            var array = plistElementDict.CreateArray("UIRequiredDeviceCapabilities");
            array.AddString("arm64");
            
            // Dictionary plist
            var dic = plistElementDict.CreateDict("NSAppTransportSecurity");
            dic.SetBoolean("NSAllowsArbitraryLoads", true);

            plist.WriteToFile(plistPath);
            
        #endregion
            
            pbxProject.WriteToFile(projectPath);
            Debug.Log($"{BuildCount} - PBXProject Written Complete");
            
            #region [Capability Manager]
            
#if UNITY_2019_3_OR_NEWER
            var manager = new ProjectCapabilityManager(projectPath, $"Entitlements.entitlements", null, targetGUID);
            // manager.AddSignInWithAppleWithCompatibility(unityFrameworkGUID);
            manager.AddPushNotifications(false);
            manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
#else
            var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", PBXProject.GetUnityTargetName());
            manager.AddSignInWithAppleWithCompatibility();
            manager.AddPushNotifications(false);
            manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
#endif
            manager.WriteToFile();
            
            #endregion
        } catch (Exception e) {
            Debug.LogError(e.Message);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FixPodfile(string buildPath) {
        var podPath = $"{buildPath}/{POD_FILE}";
        if (File.Exists(podPath) == false) {
            throw new FileNotFoundException($"Podfile is Missing. Check {nameof(podPath)} || {podPath}");
        }

        var podContent = File.ReadAllText(podPath);
        if (string.IsNullOrEmpty(podContent) == false) {
            if (POST_INSTALL_REGEX.IsMatch(podContent)) {
                if (DUPLICATE_CHECK_REGEX.IsMatch(podContent) == false) {
                    var match = INSTALLER_REGEX.Match(podContent);
                    podContent = podContent.Insert(match.Index + match.Length, APPEND_POD_CONTENT);
                }
            } else {
                podContent += APPEND_POD_CONTENT_PREFIX;
                podContent += APPEND_POD_CONTENT;
                podContent += APPEND_POD_CONTENT_SUFFIX;
            }
            File.WriteAllText(podPath, podContent);
        } else {
            throw new Exception($"Skip Install Pod Progress || {nameof(podContent)} is Null or Empty");
        }
    }
}

[BuildTypeEnum]
public enum SAMPLE_BUILD_TYPE {
    Default,
    STANDALONE,
    IOS,
}