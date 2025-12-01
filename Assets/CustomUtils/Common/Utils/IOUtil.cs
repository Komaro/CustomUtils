using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class IOUtil {
    
    public static bool TryReadText(string path, out string text) => string.IsNullOrEmpty(text = ReadText(path)) == false;

    public static string ReadText(string path) {
        try {
            path.ThrowIfNull(nameof(path));
            
            if (File.Exists(path)) {
                return File.ReadAllText(path);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return string.Empty;
    }

    public static bool TryReadText(string path, Encoding encoding, out string text) => string.IsNullOrEmpty(text = ReadText(path, encoding)) == false;

    public static string ReadText(string path, Encoding encoding) {
        try {
            path.ThrowIfNull(nameof(path));
            encoding.ThrowIfNull(nameof(encoding));

            if (File.Exists(path)) {
                return File.ReadAllText(path, encoding);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return string.Empty;
    }
    
    public static bool TryWriteText(string path, string text, out FileInfo info) => (info = WriteText(path, text)) != null;
    
    public static FileInfo WriteText(string path, string text) {
        try {
            text.ThrowIfNull(nameof(text));

            SystemUtil.EnsureDirectoryExists(path);
            File.WriteAllText(path, text);
            return new FileInfo(path);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryReadBytes(string path, out byte[] bytes) => (bytes = ReadBytes(path)) != Array.Empty<byte>();

    public static byte[] ReadBytes(string path) {
        try {
            if (File.Exists(path)) {
                return File.ReadAllBytes(path);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Array.Empty<byte>();
    }
    
    public static async IAsyncEnumerable<byte[]> ReadAllBytesParallelAsync(IEnumerable<string> paths, int bufferSize = 65536, [EnumeratorCancellation] CancellationToken token = default) {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount / 2);
        foreach (var bytes in await Task.WhenAll(paths.Select(path => CreateReadBytesTask(path, semaphore, bufferSize, token)))) {
            yield return bytes;
        }
    }

    private static Task<byte[]> CreateReadBytesTask(string path, SemaphoreSlim semaphore, int bufferSize = 65536, CancellationToken token = default) => Task.Run(async () => {
        await semaphore.WaitAsync(token);
        var bytes = await ReadBytesAsync(path, bufferSize, token);
        semaphore.Release();
        return bytes;
    }, token);

    public static async IAsyncEnumerable<byte[]> ReadAllBytesAsync(IEnumerable<string> paths, int bufferSize = 65536, [EnumeratorCancellation] CancellationToken token = default) {
        foreach (var path in paths) {
            yield return await ReadBytesAsync(path, bufferSize, token);
        }
    }

    public static async Task<byte[]> ReadBytesAsync(string path, int bufferSize = 65536, CancellationToken token = default) {
        try {
            path.ThrowIfNull(nameof(path));
            if (File.Exists(path)) {
                await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                await using var memoryStream = new MemoryStream();
                
                int readByte;
                var memory = MemoryPool<byte>.Shared.Rent(bufferSize).Memory;
                while ((readByte = await fileStream.ReadAsync(memory, token)) > 0) {
                    await memoryStream.WriteAsync(memory[..readByte], token);
                }

                return memoryStream.GetBuffer();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Array.Empty<byte>();
    }
    
    public static bool TryWriteBytes(string path, byte[] bytes, out FileInfo info) => (info = WriteBytes(path, bytes)) != null;

    public static FileInfo WriteBytes(string path, byte[] bytes) {
        try {
            bytes.ThrowIfNull(nameof(bytes));
            SystemUtil.EnsureDirectoryExists(path);
            File.WriteAllBytes(path, bytes);
            return new FileInfo(path);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }
    
    public static async IAsyncEnumerable<FileInfo> WriteAllBytesParallelAsync(IEnumerable<(string path, byte[] bytes)> locations, int bufferSize = 65536, [EnumeratorCancellation] CancellationToken token = default) {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount / 2);
        foreach (var bytes in await Task.WhenAll(locations.Select(location => CreateWriteBytesTask(location.path, location.bytes, semaphore, bufferSize, token)))) {
            yield return bytes;
        }
    }

    private static Task<FileInfo> CreateWriteBytesTask(string path, byte[] bytes, SemaphoreSlim semaphore, int bufferSize = 65536, CancellationToken token = default) => Task.Run(async () => {
        await semaphore.WaitAsync(token);
        var info = await WriteBytesAsync(path, bytes, bufferSize, token);
        semaphore.Release();
        return info;
    }, token);
    
    public static async IAsyncEnumerable<FileInfo> WriteAllBytesAsync(IEnumerable<(string path, byte[] bytes)> locations, int bufferSize = 65536, CancellationToken token = default ) {
        foreach (var (path, bytes) in locations) {
            yield return await WriteBytesAsync(path, bytes, bufferSize, token);
        }
    }

    public static async Task<FileInfo> WriteBytesAsync(string path, byte[] bytes, int bufferSize = 65536, CancellationToken token = default) {
        try {
            path.ThrowIfNull(nameof(path));
            SystemUtil.EnsureDirectoryExists(path);
            await new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.Asynchronous).WriteAsync(bytes, 0, bytes.Length, token);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static void CopyAllFiles(string sourceFolder, string targetFolder, params string[] suffixes) {
        if (Directory.Exists(sourceFolder) && Directory.Exists(targetFolder)) {
            Logger.TraceLog($"Copy Files || {sourceFolder} => {targetFolder}\n{nameof(suffixes)} || {suffixes.ToStringCollection(", ")}", Color.green);
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
}