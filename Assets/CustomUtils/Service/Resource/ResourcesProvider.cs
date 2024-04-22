using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ResourcesProvider : IResourceProvider {
    
    private readonly Dictionary<string, string> _resourcePathDic = new();
    
    private const string RESOURCES_PATH = "/Resources/";
    private const string RESOURCE_LIST_JSON = "ResourceList";
    private const int REMOVE_PREFIX_COUNT = 17;

    public void Init() {
#if UNITY_EDITOR
        // TODO. Extract Editor Module
        var resourcesFullPath = $"{Application.dataPath}/Resources";
        if (Directory.Exists(resourcesFullPath) == false) {
            Directory.CreateDirectory(resourcesFullPath);
            Logger.TraceLog($"Create Default AssetBundle Folder || Path = {resourcesFullPath}");
        }
        
        var listPath = $"{resourcesFullPath}/{RESOURCE_LIST_JSON}.json";
        if (File.Exists(listPath) == false) {
            try {
                var jObject = new JObject();
                foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                    if (Directory.Exists(path) == false && path.Contains(RESOURCES_PATH)) {
                        jObject.Add(Path.GetFileNameWithoutExtension(path).ToUpper(), path.Remove(0, REMOVE_PREFIX_COUNT).Split('.')[0]);
                    }
                }

                File.WriteAllText(listPath, jObject.ToString());
            } catch (Exception e) {
                Logger.TraceError(e);
                throw;
            }
        }
#endif
    }

    public void Load() {
        _resourcePathDic.Clear();

        var textAsset = Resources.Load<TextAsset>(RESOURCE_LIST_JSON);
        if (textAsset != null) {
            var jObject = JObject.Parse(textAsset.text);
            foreach (var pair in jObject) {
                if (pair.Value != null) {
                    _resourcePathDic.AutoAdd(pair.Key, pair.Value.ToString());
                }
            }    
        }
    }

    public Object Get(string name) => _resourcePathDic.TryGetValue(name.ToUpper(), out var path) ? Resources.Load(path) : null;
    public string GetPath(string name) => _resourcePathDic.TryGetValue(name.ToUpper(), out var path) ? path : string.Empty;
}
