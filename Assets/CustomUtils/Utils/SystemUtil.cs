﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SystemUtil {
    
    private const string WINDOWS_CMD = "cmd.exe";
    
    public static void ExecuteScript(string scriptPath, string workPath, params string[] args) {
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

    public static void ExecuteBatch(string batchPath, string workPath, params string[] args) {
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

    public static void ExecuteShell(string shellPath, string workPath, params string[] args) {
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

    public static void ExecuteProcess(ProcessStartInfo startInfo) {
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
        
            Debug.Log(stdOutBuilder);
            if (stdErrorBuilder.Length > 0) {
                Debug.LogWarning(stdErrorBuilder);
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }
    
    public static DirectoryInfo CreateDirectory(string path) {
        if (Directory.Exists(path) == false) {
            Debug.Log($"Create Directory || {path}");
             return Directory.CreateDirectory(path);
        }
        
        Debug.Log($"Already Directory Path || {path}");
        return null;
    }
    
    public static void DeleteDirectory(string path) {
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

    public static void ClearDirectory(string path) {
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
    
    public static void CopyAllFiles(string sourceFolder, string targetFolder, params string[] suffixes) {
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

    public static bool IsUnixBasePlatform() {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.MacOSX:
            case PlatformID.Unix:
                return true;
        }

        return false;
    }

    public static bool IsWindowsBasePlatform() {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                return true;
        }

        return false;
    }
}

public enum VALID_EXECUTE_EXTENSION {
    NONE,
    BAT,
    SH,
}
