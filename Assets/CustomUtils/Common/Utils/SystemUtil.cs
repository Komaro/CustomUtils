using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SystemUtil {

    #region [Script]
    
    private const string WINDOWS_CMD = "cmd.exe";

    public static void Zip(string zipPath, string destinationPath, string searchPattern = "*.*", SearchOption option = SearchOption.AllDirectories) {
        try {
            zipPath.ThrowIfNull(nameof(zipPath));
            if (Directory.Exists(zipPath) == false) {
                throw new DirectoryNotFoundException($"{nameof(zipPath)} is missing. Check {nameof(zipPath)} || {zipPath}");
            }

            if (string.IsNullOrEmpty(destinationPath) || Path.HasExtension(destinationPath) == false) {
                destinationPath = zipPath;
            }

            destinationPath.AutoSwitchExtension(Constants.Extension.ZIP);

            var isUpdate = File.Exists(destinationPath);
            using var zipArchive = ZipFile.Open(destinationPath, isUpdate ? ZipArchiveMode.Update : ZipArchiveMode.Create);
            foreach (var fileInfo in new DirectoryInfo(zipPath).EnumerateFiles(searchPattern, option)) {
                var bytes = File.ReadAllBytes(fileInfo.FullName);
                if (bytes == Array.Empty<byte>()) {
                    throw new NullReferenceException<byte[]>(nameof(bytes));
                }

                var fileRelativePath = fileInfo.FullName[(zipPath.Length + 1)..];
                if (isUpdate) {
                    zipArchive.GetEntry(fileRelativePath)?.Delete();
                }
                
                using var stream = zipArchive.CreateEntry(fileRelativePath).Open();
                stream.Write(bytes);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static async Task ZipAsync(string zipPath, string destinationPath, string searchPattern = "*.*", SearchOption option = SearchOption.AllDirectories) {
        try {
            zipPath.ThrowIfNull(nameof(zipPath));
            if (Directory.Exists(zipPath) == false) {
                throw new DirectoryNotFoundException($"{nameof(zipPath)} is missing. Check {nameof(zipPath)} || {zipPath}");
            }

            if (string.IsNullOrEmpty(destinationPath) || Path.HasExtension(destinationPath) == false) {
                destinationPath = zipPath;
            }

            destinationPath.AutoSwitchExtension(Constants.Extension.ZIP);

            var isUpdate = File.Exists(destinationPath);
            using var zipArchive = ZipFile.Open(destinationPath, isUpdate ? ZipArchiveMode.Update : ZipArchiveMode.Create);
            foreach (var fileInfo in new DirectoryInfo(zipPath).EnumerateFiles(searchPattern, option)) {
                var bytes = await IOUtil.ReadBytesAsync(fileInfo.FullName);
                if (bytes == Array.Empty<byte>()) {
                    throw new NullReferenceException<byte[]>(nameof(bytes));
                }

                var fileRelativePath = fileInfo.FullName[(zipPath.Length + 1)..];
                if (isUpdate) {
                    zipArchive.GetEntry(fileRelativePath)?.Delete();
                }
                
                await using var stream = zipArchive.CreateEntry(fileRelativePath).Open();
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void UnZip(string zipPath, string destinationPath = "", bool overwriteFiles = true, bool removeZip = false) {
        try {
            zipPath.ThrowIfNull(nameof(zipPath));
            if (File.Exists(zipPath) == false) {
                throw new FileNotFoundException($"{nameof(zipPath)} is missing. Check {nameof(zipPath)} || {zipPath}");
            }

            if (string.IsNullOrEmpty(destinationPath) || Directory.Exists(destinationPath) == false) {
                destinationPath = Path.GetDirectoryName(zipPath);
            }

            if (Directory.Exists(destinationPath) == false) {
                throw new DirectoryNotFoundException($"{nameof(destinationPath)} is missing. Check {nameof(destinationPath)} || {destinationPath}");
            }

            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Read)) {
                if (zipArchive == null) {
                    Logger.TraceError($"{nameof(zipArchive)} is null");
                    return;
                }
                
                zipArchive.ExtractToDirectory(destinationPath, overwriteFiles);
                Logger.TraceLog($"Unzip || {zipPath} to {destinationPath}", Color.green);
            }
            
            if (removeZip) {
                SafeDelete(zipPath);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void ExecuteScript(string scriptPath, string workPath, params string[] args) {
        if (string.IsNullOrEmpty(scriptPath)) {
            Logger.TraceError($"{nameof(scriptPath)} is null or empty");
            return;
        }
        
        if (File.Exists(scriptPath) == false) {
            throw new FileNotFoundException($"{nameof(scriptPath)} is missing. Check {nameof(scriptPath)} || {scriptPath}");
        }
        
        if (string.IsNullOrEmpty(workPath)) {
            workPath = Path.GetDirectoryName(scriptPath);
        }
        
        if (Directory.Exists(workPath) == false) {
            throw new DirectoryNotFoundException($"{nameof(workPath)} is missing. Check {nameof(workPath)} || {workPath}");
        }
        
        if (EnumUtil.TryConvertAllCase<VALID_EXECUTE_EXTENSION>(Path.GetExtension(scriptPath).Remove(0, 1), out var executeType)) {
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
        
        var startInfo = new ProcessStartInfo {
            FileName = WINDOWS_CMD,
            WorkingDirectory = workPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(batchPath);
        foreach (var arg in args) {
            startInfo.ArgumentList.Add(arg);
        }

        ExecuteProcess(startInfo);
    }

    public static void ExecuteShell(string shellPath, string workPath, params string[] args) {
        if (IsUnixBasePlatform() == false) {
            throw new PlatformNotSupportedException($"Invalid Platform. Check current Platform || {Environment.OSVersion.Platform}");
        }

        var startInfo = new ProcessStartInfo {
            FileName = shellPath,
            WorkingDirectory = workPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        
        foreach (var arg in args) {
            startInfo.ArgumentList.Add(arg);
        }
        
        ExecuteProcess(startInfo);
    }

    public static void ExecuteProcess(ProcessStartInfo startInfo) {
        StringUtil.StringBuilderPool.Get(out var stdOutBuilder);
        StringUtil.StringBuilderPool.Get(out var stdErrorBuilder);
        try {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            while (process.StandardOutput.EndOfStream == false) {
                stdOutBuilder.AppendLine(process.StandardOutput.ReadLine());
            }

            while (process.StandardError.EndOfStream == false) {
                stdErrorBuilder.AppendLine(process.StandardError.ReadLine());
            }

            process.WaitForExit();

            Logger.TraceLog(stdOutBuilder);
            if (stdErrorBuilder.Length > 0) {
                Logger.TraceWarning(stdErrorBuilder);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            StringUtil.StringBuilderPool.Release(stdOutBuilder);
            StringUtil.StringBuilderPool.Release(stdErrorBuilder);
        }
    }
    
    #endregion
    
    public static bool SafeDelete(string path) {
        try {
            if (File.Exists(path)) {
                File.Delete(path);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return false;
    }

    public static bool TryFindFiles(out string[] files, string directoryPath, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) => (files = FindFiles(directoryPath, searchPattern, searchOption)) != Array.Empty<string>() && files.IsNotEmpty();

    public static string[] FindFiles(string directoryPath, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        try {
            if (Directory.Exists(directoryPath)) {
                return Directory.GetFiles(directoryPath, searchPattern, searchOption);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Array.Empty<string>();
    }
    
    public static void MoveFile(string sourcePath, string targetPath, bool overwrite = true) {
        try {
            if (File.Exists(sourcePath)) {
                EnsureDirectoryExists(targetPath);
                File.Copy(sourcePath, targetPath, overwrite);
                File.Delete(sourcePath);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static void EnsureDirectoryExists(string path, bool isHidden = false) {
        try {
            var folder = path.GetAfter(Path.AltDirectorySeparatorChar);
            if (folder.StartsWith(".") == false && Path.HasExtension(folder)) {
                path = Directory.GetParent(path)?.FullName ?? path;
            }

            if (string.IsNullOrEmpty(path) == false && Directory.Exists(path) == false) {
                Directory.CreateDirectory(path);
                Logger.TraceLog($"Create directory || {path}", Color.green);

                if (isHidden && IsWindowsBasePlatform()) {
                    File.SetAttributes(path, FileAttributes.Hidden);
                    Logger.TraceLog($"Set hidden attribute || {path}", Color.yellow);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static DirectoryInfo CreateDirectory(string path, bool isHidden = false) {
        if (Directory.Exists(path) == false) {
            Logger.TraceLog($"Create directory || {path}", Color.green);
            var info = Directory.CreateDirectory(path);
            if (isHidden && IsWindowsBasePlatform()) {
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            
            return info;
        }
        
        Logger.TraceLog($"Already directory path || {path}", Color.yellow);
        return null;
    }
    
    public static void DeleteDirectory(string path) {
        if (Directory.Exists(path)) {
            Logger.TraceLog($"Remove Directory || {path}", Color.green);
            Directory.Delete(path, true);
            if (path.Contains(Application.dataPath)) {
                var metaPath = path + ".meta";
                if (File.Exists(metaPath)) {
                    File.Delete(metaPath);
                    Logger.TraceLog($"Project Inner Directory. Remove meta file || {path}", Color.yellow);
                }
            }
        }
    }

    public static void ClearDirectory(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        try {
            if (Directory.Exists(path) && TryFindFiles(out var files, path, searchPattern, searchOption)) {
                Logger.TraceLog($"Clear directory || {path}", Color.yellow);
                foreach (var file in files) {
                    File.Delete(file);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }


    #region [Create Instance]

    public static bool TryCreateInstance<T>(out T instance) where T : class {
        try {
            return (instance = Activator.CreateInstance<T>()) != null;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        instance = null;
        return false;
    }
    
    public static bool TryCreateInstance<T>(out T instance, Type type, params object[] args) where T : class {
        try {
            return (instance = Activator.CreateInstance(type, args) as T) != null;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        instance = null;
        return false;
    }
    
    public static bool TryCreateInstance(out object instance, Type type, params object[] args) {
        try {
            return (instance = Activator.CreateInstance(type, args)) != null;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        instance = null;
        return false;
    }
    
    public static bool TrySafeCreateInstance<T>(Type type, out T instance) where T : class => (instance = SafeCreateInstance<T>(type)) != null;
    public static T SafeCreateInstance<T>(Type type) where T : class => SafeCreateInstance(type) as T;

    public static bool TrySafeCreateInstance<T>(out T instance, Type type, params object[] args) where T : class => (instance = SafeCreateInstance<T>(type, args)) != null;
    public static T SafeCreateInstance<T>(Type type, params object[] args) where T : class => SafeCreateInstance(type, args) as T;

    public static bool TrySafeCreateInstance(Type type, out object instance) => (instance = SafeCreateInstance(type)) != null;

    public static object SafeCreateInstance(Type type) {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return null;
        }
#endif

        return Activator.CreateInstance(type);
    }

    public static bool TrySafeCreateInstance(out object instance, Type type, params object[] args) => (instance = SafeCreateInstance(type, args)) != null;

    public static object SafeCreateInstance(Type type, params object[] args) {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return null;
        }
#endif
        
        return Activator.CreateInstance(type, args);
    }
    
    public static bool TrySafeCreateInstance<T>(out T instance) where T : class => (instance = SafeCreateInstance<T>()) != null;

    public static T SafeCreateInstance<T>() where T : class {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return null;
        }
#endif
        
        return Activator.CreateInstance<T>();
    }
    
    #endregion

    public static bool IsUnixBasePlatform() {
        switch (Environment.OSVersion.Platform) {
            case PlatformID.MacOSX:
            case PlatformID.Unix:
                return true;
        }

        return false;
    }

    public static bool IsWindowsBasePlatform() => Environment.OSVersion.Platform switch {
        PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE => true,
        _ => false
    };
}

public enum VALID_EXECUTE_EXTENSION {
    NONE,
    BAT,
    SH,
}

