using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SystemUtil {
    
    private const string WINDOWS_CMD = "cmd.exe";

    public static void ExecuteScript(string scriptPath, string workPath, params string[] args) {
        if (string.IsNullOrEmpty(scriptPath)) {
            Logger.TraceError($"{nameof(scriptPath)} is Null or Empty");
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
        
            Logger.TraceLog(stdOutBuilder);
            if (stdErrorBuilder.Length > 0) {
                Logger.TraceWarning(stdErrorBuilder);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
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
            throw;
        }
    }

    public static void EnsureDirectoryExists(string path) {
        try {
            if (Constants.Regex.FILE_PATH_REGEX.IsMatch(path)) {
                path = Directory.GetParent(path)?.FullName ?? path;
            }

            if (Constants.Regex.FOLDER_PATH_REGEX.IsMatch(path) && Directory.Exists(path) == false) {
                Logger.TraceLog($"Create Directory || {path}", Color.green);
                Directory.CreateDirectory(path);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }
    
    public static DirectoryInfo CreateDirectory(string path) {
        if (Directory.Exists(path) == false) {
            Logger.TraceLog($"Create Directory || {path}", Color.green);
            return Directory.CreateDirectory(path);
        }
        
        Logger.TraceLog($"Already Directory Path || {path}", Color.yellow);
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

    public static void ClearDirectory(string path) {
        if (Directory.Exists(path)) {
            Logger.TraceLog($"Clear Directory || {path}", Color.red);
            foreach (var filePath in Directory.GetFiles(path)) {
                File.Delete(filePath);
            }

            foreach (var directoryPath in Directory.GetDirectories(path)) {
                DeleteDirectory(directoryPath);
            }
        }
    }

    public static bool TryReadAllText(string path, out string text) {
        try {
            if (File.Exists(path)) {
                text = File.ReadAllText(path);
                return true;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        text = string.Empty;
        return false;
    }

    public static bool TryReadAllBytes(string path, out byte[] bytes) {
        try {
            if (File.Exists(path)) {
                bytes = File.ReadAllBytes(path);
                return bytes is { Length: > 0 };
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        bytes = Array.Empty<byte>();
        return false;
    }

    public static bool TryWriteAllBytes(string path, byte[] bytes, out FileInfo info) => (info = WriteAllBytes(path, bytes)) != null;

    public static FileInfo WriteAllBytes(string path, byte[] bytes) {
        if (bytes == null) {
            return null;
        }
        
        try {
            EnsureDirectoryExists(path);
            File.WriteAllBytes(path, bytes);
            return new FileInfo(path);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
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
    
    public static bool TrySafeCreateInstance<T>(out T instance) {
        instance = SafeCreateInstance<T>();
        return instance != null;
    }

    public static T SafeCreateInstance<T>() {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return default;
        }
#endif
        
        return Activator.CreateInstance<T>();
    }

    public static bool TrySafeCreateInstance<T>(Type type, out T instance) where T : class {
        instance = SafeCreateInstance<T>(type);
        return instance != default;
    }
    
    public static T SafeCreateInstance<T>(Type type) where T : class {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return default;
        }
#endif  
        return Activator.CreateInstance(type) as T;
    }

    public static bool TrySafeCreateInstance<T>(out T instance, Type type, params object[] args) where T : class {
        instance = SafeCreateInstance<T>(type, args);
        return instance != default;
    }
    
    public static T SafeCreateInstance<T>(Type type, params object[] args) where T : class {
        if (args is { Length: <= 0 }) {
            Logger.TraceError($"{nameof(args)} is null or empty");
            return default;
        }
    
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return default;
        }
#endif
        
        return Activator.CreateInstance(type, args) as T;
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

