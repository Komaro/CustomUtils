using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BuildManager : IPostprocessBuildWithReport {

    private static Builder _builder;
    
    public int callbackOrder => 1000;

    private static string _unityProjectPath;
    public static string UNITY_PROJECT_PATH => string.IsNullOrEmpty(_unityProjectPath) ? _unityProjectPath = Application.dataPath.Replace("/Assets", string.Empty) : _unityProjectPath;
    
    public static Builder CreateBuilder(Enum buildType) {
        var builderType = ReflectionManager.GetSubClassTypes<Builder>()?.Where(x => x.GetCustomAttribute<BuilderAttribute>()?.buildType.Equals(buildType) ?? false).FirstOrDefault();
        if (builderType != null && Activator.CreateInstance(builderType) is Builder builder) {
            _builder = builder;
            return _builder;
        }

        Debug.LogError($"{nameof(buildType)} is Missing enum type. Implement {nameof(Builder)} and {nameof(BuilderAttribute)}");
        return null;
    }

    public static bool TryCreateBuilder(Enum buildType, out Builder builder) {
        _builder = builder = CreateBuilder(buildType);
        return _builder != null;
    }

    public void OnPostprocessBuild(BuildReport report) {
        if (_builder == null) {
            Debug.LogError($"{nameof(_builder)} is Null");
            return;
        }

        try {
            _builder.PostProcess();
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        } finally {
            _builder = null;
        }
    }

    public static void BuildOnCLI() {
        try {
            Debug.Log($"=============== Start {nameof(BuildOnCLI)} ===============");
            BuildSettings.SetBuildSettingsOnCLI();
            if (BuildSettings.Instance.TryGetValue<string>("buildType", out var typeText)) {
                var enumType = ReflectionManager.GetAttributeEnumTypes<BuildTypeAttribute>().First()?.GetEnumValues()?.GetValue(0)?.GetType();
                if (Enum.TryParse(enumType, typeText, out var typeObject) && typeObject is Enum buildType) {
                    _builder = CreateBuilder(buildType);
                    
                    var buildOptions = new BuildPlayerOptions {
                        scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray()
                    };
                    
                    if (BuildSettings.Instance.TryGetValue<string>("buildPath", out var buildPath) == false) {
                        buildPath = $"{UNITY_PROJECT_PATH}/Build/{buildType}";
                    }
                    
                    if (Directory.Exists(buildPath) == false) {
                        Directory.CreateDirectory(buildPath);
                    }
                    
                    buildOptions.locationPathName = buildPath;
                    Debug.Log($"Build Path : {buildOptions.locationPathName}");
                    
                    _builder.StartBuild(buildOptions);
                }
            }
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }
}

public abstract class Builder {

    private int _countNum = 0;
    private int CountNum => ++_countNum;
    protected string BuildCount => $"[{GetType().Name}] ({CountNum})";

    protected BuildPlayerOptions buildOptions;
    
    protected string ProjectPath => Directory.GetParent(Application.dataPath)?.FullName;
    protected string BuildPath => buildOptions.locationPathName;
    protected string BuildParentPath => Directory.GetParent(buildOptions.locationPathName)?.FullName;
    protected string BuildExecutePath => $"{ProjectPath}/{BUILD_EXECUTE_FOLDER}/{GetType()?.GetCustomAttribute<BuilderAttribute>()?.buildType.ToString()}";

    protected const string BUILD_EXECUTE_FOLDER = "BuildExecute";
    protected const string RESOURCES_ROOT_FOLDER = "Assets/Resources/";
    protected const string WINDOWS_CMD = "cmd.exe";

    public virtual void StartBuild(BuildPlayerOptions buildOptions) {
        this.buildOptions = buildOptions;

        var attribute = GetType().GetCustomAttribute<BuilderAttribute>();
        if (attribute != null) {
            buildOptions.target = attribute.buildTarget;
            buildOptions.targetGroup = attribute.buildTargetGroup;
        } else {
            Debug.LogWarning($"{nameof(attribute)} is Null. Checking {nameof(BuilderAttribute)}");  
        }
        
        PreProcess();
        AssetDatabase.Refresh();
        
        BuildPipeline.BuildPlayer(buildOptions);
    }

    protected void PreProcess() {
        Debug.Log($"{BuildCount} - Preprocess");
        CommonPreProcess();
        OnPreProcess();
    }

    public void PostProcess() {
        Debug.Log($"{BuildCount} - Postprocess");
        OnPostProcess();

        if (BuildSettings.Instance.TryGetValue<bool>("cleanBurstDebug", out var isClean) && isClean) {
            ClearBurstDebug(BuildParentPath);
        }

        if (BuildSettings.Instance.TryGetValue<bool>("cleanIL2CPPSludge", out isClean) && isClean) {
            ClearIL2CPPSludge(BuildParentPath);
        }

        if (BuildSettings.Instance.TryGetValue<bool>("revealInFinder", out var isShow) && isShow) {
            EditorUtility.RevealInFinder(BuildPath);
        }
    }
    
    private void CommonPreProcess() { }
    
    protected abstract void OnPreProcess();
    protected abstract void OnPostProcess();

    #region [Common]
    
    protected void SetApplicationId(string appId) {
        if (string.IsNullOrEmpty(appId)) {
            Debug.LogError($"{nameof(appId)} is Null or Empty.");
            return;
        }

        PlayerSettings.applicationIdentifier = appId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.applicationIdentifier)} || {PlayerSettings.applicationIdentifier}");
    }

    protected void SetApplicationIdentifier(BuildTargetGroup targetGroup, string identifier) {
        PlayerSettings.SetApplicationIdentifier(targetGroup, identifier);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.SetApplicationIdentifier)} || {PlayerSettings.GetApplicationIdentifier(targetGroup)}");
    }

    protected void SetVersionName(string version) {
        if (string.IsNullOrEmpty(version)) {
            Debug.LogError($"{nameof(version)} is Null or Empty");
            return;
        }

        if (version.Split('.').Length < 3 || PlayerSettings.bundleVersion.Split('.').Length < 3) {
            Debug.LogError($"Invalid {nameof(version)} || {version}");
            return;
        }

        PlayerSettings.bundleVersion = version;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.bundleVersion)} || {PlayerSettings.bundleVersion}");
    }

    protected void SetDefineSymbols(BuildTargetGroup targetGroup, string symbols) {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.SetScriptingDefineSymbols)} || {PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup)}");
    }
    
    protected void SetDevelopmentBuild(bool isActive) {
        EditorUserBuildSettings.development = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.development)} || {EditorUserBuildSettings.development}");
    }

    protected void SetAutoConnectProfile(bool isActive) {
        EditorUserBuildSettings.connectProfiler = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.connectProfiler)} || {EditorUserBuildSettings.connectProfiler}");
    }

    protected void SetDeepProfilingSupport(bool isActive) {
        EditorUserBuildSettings.buildWithDeepProfilingSupport = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.buildWithDeepProfilingSupport)} || {EditorUserBuildSettings.buildWithDeepProfilingSupport}");
    }

    protected void SetScriptDebugging(bool isActive) {
        EditorUserBuildSettings.allowDebugging = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.allowDebugging)} || {EditorUserBuildSettings.allowDebugging}");
    }

    protected void SetScriptBackend(BuildTargetGroup targetGroup, ScriptingImplementation backend) {
        PlayerSettings.SetScriptingBackend(targetGroup, backend);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetScriptingBackend)} || {PlayerSettings.GetScriptingBackend(targetGroup)}");
    }

    protected void SetManagedStrippingLevel(BuildTargetGroup targetGroup, ManagedStrippingLevel strippingLevel) {
        PlayerSettings.SetManagedStrippingLevel(targetGroup, strippingLevel);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetManagedStrippingLevel)} || {PlayerSettings.GetManagedStrippingLevel(targetGroup)}");
    }

    // NET_Unity_4_8 = .NET Framework
    // NET_Standard_2_0 = .NET Standard 2.1
    protected void SetApiCompatibilityLevel(BuildTargetGroup targetGroup, ApiCompatibilityLevel apiLevel) {
        PlayerSettings.SetApiCompatibilityLevel(targetGroup, apiLevel);
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.GetApiCompatibilityLevel)} || {PlayerSettings.GetApiCompatibilityLevel(targetGroup)}");
    }

    protected void SetConditionBuildOptions(ref BuildPlayerOptions refBuildOptions, bool isCondition, BuildOptions type) {
        if (isCondition) {
            refBuildOptions.options |= type;
        } else {
            refBuildOptions.options &= ~type;
        }
    }

    #endregion

    #region [Android]

    protected void SetVersionCode(int code) {
        PlayerSettings.Android.bundleVersionCode = code;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.bundleVersionCode)} || {PlayerSettings.Android.bundleVersionCode}");
    }
    
    protected void SetKeystoreName(string name) {
        PlayerSettings.Android.keystoreName = name;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keystoreName)} || {PlayerSettings.Android.keystoreName}");
    }

    protected void SetKeystorePass(string pass) {
        PlayerSettings.Android.keystorePass = pass;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasPass)} || ****");
    }

    protected void SetKeyAliasName(string name) {
        PlayerSettings.Android.keyaliasName = name;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasName)} || {PlayerSettings.Android.keyaliasName}");
    }

    protected void SetKeyAliasPass(string pass) {
        PlayerSettings.Android.keyaliasPass = pass;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.keyaliasPass)} || ****");
    }

    protected void SetAAB(bool isActive) {
        EditorUserBuildSettings.buildAppBundle = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.buildAppBundle)} || {EditorUserBuildSettings.buildAppBundle}");
    }

    protected void SetBuildApkPerCpuArchitecture(bool isActive) {
        PlayerSettings.Android.buildApkPerCpuArchitecture = isActive;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.Android.buildApkPerCpuArchitecture)} || {PlayerSettings.Android.buildApkPerCpuArchitecture}");
    }

    protected void SetExportAsGoogleAndroidProject(bool isActive) {
        EditorUserBuildSettings.exportAsGoogleAndroidProject = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.exportAsGoogleAndroidProject)} || {EditorUserBuildSettings.exportAsGoogleAndroidProject}");
    }

#if UNITY_2021_1_OR_NEWER
    protected void SetAndroidCreateSymbol(AndroidCreateSymbols symbol) {
        EditorUserBuildSettings.androidCreateSymbols = symbol;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.androidCreateSymbols)} || {EditorUserBuildSettings.androidCreateSymbols}");
    }
#else
    protected void SetAndroidCreateSymbol(bool isActive) {
        EditorUserBuildSettings.androidCreateSymbolsZip = isActive;
        Debug.Log($"{BuildCount} - {nameof(EditorUserBuildSettings.androidCreateSymbolsZip)} || {EditorUserBuildSettings.androidCreateSymbolsZip}");
    }
#endif

    #endregion
    
    #region [iOS]

    protected void SetBuildNumber(string buildNumber) {
        PlayerSettings.iOS.buildNumber = buildNumber;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.buildNumber)} || {PlayerSettings.iOS.buildNumber}");
    }

    protected void SetTargetOSVersion(string targetVersion) {
        PlayerSettings.iOS.targetOSVersionString = targetVersion;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.targetOSVersionString)} || {PlayerSettings.iOS.targetOSVersionString}");
    }

    protected void SetAppleEnableAutomaticSigning(bool isAutomatic) {
        PlayerSettings.iOS.appleEnableAutomaticSigning = isAutomatic;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.appleEnableAutomaticSigning)} || {PlayerSettings.iOS.appleEnableAutomaticSigning}");
    }

    protected void SetAppleDeveloperTeamId(string teamId) {
        PlayerSettings.iOS.appleDeveloperTeamID = teamId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.appleDeveloperTeamID)} || {PlayerSettings.iOS.appleDeveloperTeamID}");
    }

    protected void SetProvisioningProfileType(ProvisioningProfileType type) {
        PlayerSettings.iOS.iOSManualProvisioningProfileType = type;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.iOSManualProvisioningProfileType)} || {PlayerSettings.iOS.iOSManualProvisioningProfileType}");
    }

    protected void SetiOSManualProvisioningProfileID(string provisioningId) {
        PlayerSettings.iOS.iOSManualProvisioningProfileID = provisioningId;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.iOSManualProvisioningProfileID)} || {PlayerSettings.iOS.iOSManualProvisioningProfileID}");
    }

    protected void SetHideHomeButton(bool isHide) {
        PlayerSettings.iOS.hideHomeButton = isHide;
        Debug.Log($"{BuildCount} - {nameof(PlayerSettings.iOS.hideHomeButton)} || {PlayerSettings.iOS.hideHomeButton}");
    }

    #endregion

    #region [Utils]
    
    protected virtual void ExecuteScript(string scriptPath, string workPath, params string[] args) {
        if (string.IsNullOrEmpty(scriptPath)) {
            Debug.LogError($"{nameof(scriptPath)} is Null or Empty");
            return;
        }
        
        if (File.Exists(scriptPath) == false) {
            throw new FileNotFoundException($"{nameof(scriptPath)} is Missing. Check {nameof(scriptPath)} || {scriptPath}");
        }
        
        if (string.IsNullOrEmpty(workPath)) {
            workPath = Path.GetDirectoryName(scriptPath);
        }
        
        if (Directory.Exists(workPath) == false) {
            throw new DirectoryNotFoundException($"{nameof(workPath)} is Missing. Check {nameof(workPath)} || {workPath}");
        }
        
        if (EnumUtil.TryGetValueAllCase<VALID_EXECUTE_EXTENSION>(Path.GetExtension(scriptPath).Remove(0, 1), out var executeType)) {
            switch (executeType) {
                case VALID_EXECUTE_EXTENSION.BAT:
                    ExecuteBatch(scriptPath, workPath, args);
                    break;
                case VALID_EXECUTE_EXTENSION.SH:
                    ExecuteShell(scriptPath, workPath, args);
                    break;
            }
        }
    }

    protected virtual void ExecuteBatch(string batchPath, string workPath, params string[] args) {
        if (IsWindowsBasePlatform() == false) {
            throw new PlatformNotSupportedException($"Invalid Platform. Check current Platform || {Environment.OSVersion.Platform}");
        }
        
        var startInfo = new ProcessStartInfo() {
            FileName = WINDOWS_CMD,
            WorkingDirectory = workPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(batchPath);
        startInfo.ArgumentList.AddRange(args);
        
        ExecuteProcess(startInfo);
    }

    protected virtual void ExecuteShell(string shellPath, string workPath, params string[] args) {
        if (IsUnixBasePlatform() == false) {
            throw new PlatformNotSupportedException($"Invalid Platform. Check current Platform || {Environment.OSVersion.Platform}");
        }

        var startInfo = new ProcessStartInfo() {
            FileName = shellPath,
            WorkingDirectory = workPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        
        startInfo.ArgumentList.AddRange(args);
        
        ExecuteProcess(startInfo);
    }

    protected virtual void ExecuteProcess(ProcessStartInfo startInfo) {
        try {
            using var process = new Process{ StartInfo = startInfo };
            process.Start();
        
            var stdOutBuilder = new StringBuilder();
            while (process.StandardOutput.EndOfStream == false) {
                stdOutBuilder.AppendLine(process.StandardOutput.ReadLine());
            }
        
            var stdErrorBuilder = new StringBuilder();
            while (process.StandardError.EndOfStream == false) {
                stdErrorBuilder.AppendLine(process.StandardError.ReadLine());
            }
        
            process.WaitForExit();
        
            Debug.Log($"{BuildCount} - {stdOutBuilder}");
            if (stdErrorBuilder.Length > 0) {
                Debug.LogWarning($"{BuildCount} - {stdErrorBuilder}");
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }
    
    protected void ClearBurstDebug(string path) {
        path = Path.GetFullPath(path);
        foreach (var directoryPath in Directory.GetDirectories(path)) {
            if (directoryPath.Contains("DoNotShip")) {
                DeleteDirectory(directoryPath);
            } else {
                ClearBurstDebug(directoryPath);
            } 
        }
    }

    protected void ClearIL2CPPSludge(string path) {
        path = Path.GetFullPath(path);
        foreach (var directoryPath in Directory.GetDirectories(path)) {
            if (directoryPath.Contains("ButDontShipItWithYourGame")) {
                DeleteDirectory(directoryPath);
            } else {
                ClearIL2CPPSludge(directoryPath);
            }
        }
    }

    protected DirectoryInfo CreateDirectory(string path) {
        if (Directory.Exists(path) == false) {
            Debug.Log($"Create Directory || {path}");
             return Directory.CreateDirectory(path);
        } else {
            Debug.Log($"Already Directory Path || {path}");
        }
        
        return null;
    }
    
    protected void DeleteDirectory(string path) {
        if (Directory.Exists(path)) {
            Debug.Log($"Remove Directory || {path}");
            Directory.Delete(path, true);
            if (path.Contains(Application.dataPath)) {
                var metaPath = path + ".meta";
                if (File.Exists(metaPath)) {
                    File.Delete(metaPath);
                    Debug.Log($"Project Inner Directory. Remove meta file || {path}");
                }
            }
        }
    }
    
    protected void ClearDirectory(string path) {
        if (Directory.Exists(path)) {
            Debug.Log($"Clear Directory || {path}");
            foreach (var filePath in Directory.GetFiles(path)) {
                File.Delete(filePath);
            }

            foreach (var directoryPath in Directory.GetDirectories(path)) {
                DeleteDirectory(directoryPath);
            }
        }
    }
    
    protected void CopyAllFiles(string sourceFolder, string targetFolder, params string[] suffixes) {
        if (Directory.Exists(sourceFolder) && Directory.Exists(targetFolder)) {
            Debug.Log($"Copy Files || {sourceFolder} => {targetFolder}\n{nameof(suffixes)} || {suffixes.ToStringCollection(", ")}");
            var filePaths = Directory.GetFiles(sourceFolder);
            if (filePaths.Length > 0) {
                foreach (var filePath in filePaths) {
                    if (suffixes.Length > 0 && suffixes.Any(suffix => filePath.EndsWith(suffix)) == false) {
                        continue;
                    }
                    
                    File.Copy(filePath, Path.Combine(targetFolder, Path.GetFileName(filePath)));
                }
            }
        }
    }

    protected bool IsUnixBasePlatform() {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.MacOSX:
            case PlatformID.Unix:
                return true;
        }

        return false;
    }

    protected bool IsWindowsBasePlatform() {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                return true;
        }

        return false;
    }
    
    #endregion
}

//******** Sample Enum Attribute ********//
/*
[BuildType]
public enum BUILD_TYPE {
    NONE,
    ANDROID,
    IOS
}

[BuildOption(BuildTargetGroup.Unknown)]
public enum COMMON_BUILD_OPTION_TYPE {
    [EnumValue("\n\n=== Common Build Option ===")]
    IL2Cpp,
}

[BuildOption(BuildTargetGroup.iOS)]
public enum IOS_BUILD_OPTION_TYPE {
    [EnumValue("\n\n=== iOS Build Option ===")]
    DEBUG_IOS,
}

[DefineSymbol]
public enum BUILD_DEFINE_SYMBOL_TYPE {
    [EnumValue("\n\n=== DEBUG ===")]
    DEBUG_LOG
}
*/

[AttributeUsage(AttributeTargets.Class)]
public class BuilderAttribute : Attribute {
    
    public Enum buildType;
    public BuildTarget buildTarget;
    public BuildTargetGroup buildTargetGroup;
    
    /// <param name="buildType">enum Value</param>
    /// <param name="buildTarget"></param>
    /// <param name="buildTargetGroup"></param>
    public BuilderAttribute(object buildType, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup) {
        if (buildType is Enum enumType) {
            this.buildType = enumType;
        } else {
            this.buildType = default;
        }
        
        this.buildTarget = buildTarget;
        this.buildTargetGroup = buildTargetGroup;
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class BuildTypeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Enum)]
public class BuildOptionAttribute : Attribute {
    
    public BuildTargetGroup buildTargetGroup;

    public BuildOptionAttribute(BuildTargetGroup buildTargetGroup) {
        this.buildTargetGroup = buildTargetGroup;
    }
}

[AttributeUsage(AttributeTargets.Enum)]
public class DefineSymbolAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class EnumValueAttribute : Attribute {
    
    public string divideText;

    public EnumValueAttribute(string divideText = "") {
        this.divideText = divideText;
    }
}

public enum VALID_EXECUTE_EXTENSION {
    NONE,
    BAT,
    SH,
}
