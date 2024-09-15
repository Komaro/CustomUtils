using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EditorLiappExporter : AssetPostprocessor {

    private static readonly List<string> SUFFIX_LIST = new() { ".ipa", ".aab", ".apk" };

    private const string IPA_SUFFIX = ".ipa";
    private const string AAB_SUFFIX = ".aab";
    private const string APK_SUFFIX = ".apk";

    private const string LIAPP_WINDOWS_AAB_SIGNING = "Liapp_aab_Signing.bat";
    private const string LIAPP_WINDOWS_APK_SIGNING = "Liapp_apk_Signing.bat";
    
    private const string LIAPP_MAC_IPA_SIGNING = "Liapp_ipa_Signing.sh";
    private const string LIAPP_MAC_AAB_SIGNING = "Liapp_aab_Signing.sh";
    private const string LIAPP_MAC_APK_SIGNING = "Liapp_apk_Signing.sh";

    private const string LIAPP_EXPORTER_FOLDER = "Liapp";
    private const string LIAPP_EXPORT_FOLDER = "LiappExport";

    private const string ZIP_ALIGN_NAME = "zipalign";
    private const string APK_SIGNER_NAME = "apksigner";

    private const string WINDOWS_CMD = "cmd.exe";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        var catchList = importedAssets.Where(path => SUFFIX_LIST.Any(path.EndsWith)).ToList();
        if (catchList.Count > 0) {
            if (EditorUtility.DisplayDialog($"Liapp Resigning", $"에셋에서 [{catchList.Select(Path.GetFileName).ToStringCollection("] [")}] 파일이 확인하였습니다. Liapp Resigning을 진행합니까?", "예", "아니오")) {
                foreach (var path in catchList) {
                    Progress(Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty, path));
                }

                if (EditorUtility.DisplayDialog("Liapp Resigning", $"Liapp Resigning 완료\n경로 : {Directory.GetParent(Application.dataPath)?.FullName}/{LIAPP_EXPORT_FOLDER}", "바로가기", "확인")) {
                    EditorUtility.RevealInFinder($"{Directory.GetParent(Application.dataPath)?.FullName}/{LIAPP_EXPORT_FOLDER}");
                }
            }
        }
    }
    
    private static void Progress(string path) {
        if (File.Exists(path)) {
            var exportPath = $"{Directory.GetParent(Application.dataPath)?.FullName}/{LIAPP_EXPORT_FOLDER}";
            if (Directory.Exists(exportPath) == false) {
                if (EditorUtility.DisplayDialog("Warning", "Liapp Resigning 결과를 저장할 폴더가 없습니다.\n확인 시 폴더를 생성하고 Resigning을 진행합니다.\n취소 시 Resigning이 작업 자체를 취소합니다.", "확인", "취소")) {
                    Directory.CreateDirectory(exportPath);
                    ExecuteExporter(path, exportPath);
                    return;
                }
            }
            
            ExecuteExporter(path, exportPath);
        } else {
            Debug.LogError($"Missing || {path}");
        }
    }

    private static void ExecuteExporter(string path, string exportPath) {
        var startInfo = new ProcessStartInfo {
            WorkingDirectory = Path.GetDirectoryName(path) ?? exportPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var fileName = Path.GetFileName(path);
        var projectPath = $"{Application.dataPath}/OutScript/Liapp";
        var exporterPath = $"{projectPath}/{LIAPP_EXPORTER_FOLDER}";
        var keyStore = PlayerSettings.Android.keystoreName;
        var keyStorePass = PlayerSettings.Android.keystorePass;
        var keyStoreAlias = PlayerSettings.Android.keyaliasName;
        if (fileName.EndsWith(IPA_SUFFIX)) {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    var liappExporterPath = Path.Combine(exporterPath, LIAPP_MAC_IPA_SIGNING);
                    var ipaResigningDevelopment = ""; // TODO. Get Local Save Data
                    if (File.Exists(liappExporterPath)) {
                        startInfo.FileName = liappExporterPath;
                        startInfo.ArgumentList.Add(ipaResigningDevelopment);
                        startInfo.ArgumentList.Add(path);
                        startInfo.ArgumentList.Add(exportPath);
                    }
                    break;
                default:
                    Debug.LogError($"Only {nameof(PlatformID.MacOSX)} or {nameof(PlatformID.Unix)} OS Available || {path}");
                    return;
            }
        } else if (fileName.EndsWith(AAB_SUFFIX)) {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Win32NT:
                    var liappExporterPath = Path.Combine(exporterPath, LIAPP_WINDOWS_AAB_SIGNING);
                    if (File.Exists(liappExporterPath)) {
                        startInfo.FileName = WINDOWS_CMD;
                        
                        // Execute .bat
                        startInfo.ArgumentList.Add("/c");
                        startInfo.ArgumentList.Add(liappExporterPath.Replace("/", "\\"));
                        
                        // Input .bat Argument
                        startInfo.ArgumentList.Add(path.Replace("/", "\\"));
                        startInfo.ArgumentList.Add(Path.Combine(projectPath ?? string.Empty, keyStore).Replace("/", "\\"));
                        startInfo.ArgumentList.Add(keyStorePass.Replace("/", "\\"));
                        startInfo.ArgumentList.Add(keyStoreAlias);
                        startInfo.ArgumentList.Add(exportPath.Replace("/", "\\"));
                    }
                    break;
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    liappExporterPath = Path.Combine(exporterPath, LIAPP_MAC_AAB_SIGNING);
                    if (File.Exists(liappExporterPath)) {
                        startInfo.FileName = liappExporterPath;
                        startInfo.ArgumentList.Add(path);
                        startInfo.ArgumentList.Add(Path.Combine(projectPath ?? string.Empty, keyStore));
                        startInfo.ArgumentList.Add(keyStorePass);
                        startInfo.ArgumentList.Add(keyStoreAlias);
                        startInfo.ArgumentList.Add(exportPath);
                    }
                    break;
                default:
                    Debug.LogError($"Invalid {nameof(Environment.OSVersion.Platform)} || {Environment.OSVersion.Platform}");
                    return;
            }
        } else if (fileName.EndsWith(APK_SUFFIX)) {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Win32NT:
                    var liappExporterPath = Path.Combine(exporterPath, LIAPP_WINDOWS_APK_SIGNING);
                    if (File.Exists(liappExporterPath)) {
                        startInfo.FileName = WINDOWS_CMD;
                        
                        // Execute .bat
                        startInfo.ArgumentList.Add("/c");
                        startInfo.ArgumentList.Add(liappExporterPath.Replace("/", "\\"));
                        
                        // Input .bat Argument
                        startInfo.ArgumentList.Add(path.Replace("/", "\\"));
                        startInfo.ArgumentList.Add(Path.Combine(projectPath ?? string.Empty, keyStore).Replace("/", "\\"));
                        startInfo.ArgumentList.Add(keyStorePass.Replace("/", "\\"));
                        startInfo.ArgumentList.Add(keyStoreAlias);
                        startInfo.ArgumentList.Add(exportPath.Replace("/", "\\"));
                        startInfo.ArgumentList.Add(GetApkSignerPath().Replace("/", "\\"));
                    }
                    break;
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    liappExporterPath = Path.Combine(exporterPath, LIAPP_MAC_APK_SIGNING);
                    if (File.Exists(liappExporterPath)) {
                        startInfo.FileName = liappExporterPath;
                        startInfo.ArgumentList.Add(path);
                        startInfo.ArgumentList.Add(Path.Combine(projectPath ?? string.Empty, keyStore));
                        startInfo.ArgumentList.Add(keyStorePass);
                        startInfo.ArgumentList.Add(keyStoreAlias);
                        startInfo.ArgumentList.Add(exportPath);
                        startInfo.ArgumentList.Add(GetZipAlignPath());
                        startInfo.ArgumentList.Add(GetApkSignerPath());
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(startInfo.FileName)) {
            EditorUtility.DisplayDialog("Error", $"타겟이 되는 Liapp [{SUFFIX_LIST.ToStringCollection(" ")}] 파일이 없습니다.", "확인");
            return;
        }
        
        using var process = new Process { StartInfo = startInfo };
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

        if (stdOutBuilder.Length > 0) {
            Debug.Log(stdOutBuilder);
        }

        if (stdErrorBuilder.Length > 0) {
            Debug.LogWarning(stdErrorBuilder);
        }
    }

    private static string GetZipAlignPath() {
        var sdkRoot = AndroidExternalToolsSettings.sdkRootPath;
        if (string.IsNullOrEmpty(sdkRoot)) {
            Debug.LogError($"{nameof(sdkRoot)} is Null or Empty, Checking Path {nameof(AndroidExternalToolsSettings)} {nameof(AndroidExternalToolsSettings.sdkRootPath)}");
            return string.Empty;
        }

        if (Directory.Exists(sdkRoot)) {
            var zipAligns = EditorCommon.SearchFilePath(sdkRoot, ZIP_ALIGN_NAME);
            if (zipAligns != null && zipAligns.Count > 0) {
                return zipAligns.First();
            }
        }

        return string.Empty;
    }
    
    private static string GetApkSignerPath() {
        var sdkRoot = AndroidExternalToolsSettings.sdkRootPath;
        if (string.IsNullOrEmpty(sdkRoot)) {
            Debug.LogError($"{nameof(sdkRoot)} is Null or Empty, Checking {nameof(AndroidExternalToolsSettings)} {nameof(AndroidExternalToolsSettings.sdkRootPath)}");
            return string.Empty;
        }

        if (Directory.Exists(sdkRoot)) {
            var apkSigners = EditorCommon.SearchFilePath(sdkRoot, APK_SIGNER_NAME);
            if (apkSigners != null && apkSigners.Count > 0) {
                return apkSigners.First();
            }
        }
        
        return string.Empty;
    }
}