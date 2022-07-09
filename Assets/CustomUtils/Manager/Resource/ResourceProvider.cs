using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using UnityEditor;
using UnityEngine;

public interface IResourceProvider {
    void Init();
    Dictionary<string, Object> Load();
    Object Get(string name);
}

public class LocalResourceProvider : IResourceProvider {
    
    private Dictionary<string, string> _resourcePathDic = new Dictionary<string, string>();
    
    private const string RESOURCE_PATH = "Assets/AssetBundles";
    private const string META_SUFFIX = ".meta";
    private readonly List<string> VALID_TYPE = new List<string> { ".prefab", ".spriteatlas" };

    public void Init() {
#if UNITY_EDITOR
        if (Directory.Exists(RESOURCE_PATH) == false) {
            Directory.CreateDirectory(RESOURCE_PATH);
            Logger.TraceLog($"Create Default AssetBundle Folder || Path = {RESOURCE_PATH}");
        }
#endif
    }

    public void Clear() {
        _resourcePathDic.Clear();
    }

    public Dictionary<string, Object> Load() {
        Clear();
        LoadFile(RESOURCE_PATH);
        LoadFolder(RESOURCE_PATH);

        return null;
    }

    private void LoadFile(string path) {
        var fileList = Directory.GetFiles(path);
        foreach (var file in fileList) {
            if (file.Contains(META_SUFFIX)) {
                continue;
            }
            
            if (VALID_TYPE.Exists(x => file.Contains(x))) {
                _resourcePathDic.Add(Path.GetFileNameWithoutExtension(file), file.Replace("\\", "/"));
            }
        }
    }

    private void LoadFolder(string path) {
        var folderList = Directory.GetDirectories(path);
        foreach (var folder in folderList) {
            var directoryInfo = new DirectoryInfo(folder);
            if ((directoryInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) {
                continue;
            }
            
            LoadFile(folder);
            LoadFolder(folder);
        }
    }

    public Object Get(string name) {
        if (_resourcePathDic.TryGetValue(name, out var path)) {
            return AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }
        return null;
    }
}
