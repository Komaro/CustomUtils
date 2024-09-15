using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[ResourceProvider(9999)]
[ResourceSubProvider(10)]
public class ResourcesProvider : IResourceProvider {
    
    private readonly ConcurrentDictionary<string, string> _pathDic = new();
    private readonly ConcurrentDictionary<string, Object> _cacheDic = new();

    public void Init() {
        _pathDic.Clear();
        var textAsset = Resources.Load<TextAsset>(Constants.Resource.RESOURCE_LIST);
        if (textAsset != null) {
            var jObject = JObject.Parse(textAsset.text);
            foreach (var pair in jObject) {
                if (pair.Value != null) {
                    _pathDic.AutoAdd(pair.Key, pair.Value.ToString());
                }
            }
        }
    }
    
    public void Clear() {
        _pathDic.Clear();
        _cacheDic.Clear();
        Resources.UnloadUnusedAssets();
    }

    public void ExecuteOrder(ResourceOrder order) { }
    public void Load(ResourceOrder order) { }
    public void Unload(ResourceOrder order) { }

    public Object Get(string name) {
        name = name.ToUpper();
        if (_cacheDic.TryGetValue(name, out var ob)) {
            return ob;
        }

        if (_pathDic.TryGetValue(name, out var path)) {
            ob = Resources.Load(path);
        }

        return ob;
    }

    public Object Get(ResourceOrder order) {
        switch (order) {
            case ResourcesOrder resourcesOrder:
                return Get(resourcesOrder.assetName);
        }

        return null;
    }

    public string GetPath(string name) => _pathDic.TryGetValue(name.ToUpper(), out var path) ? path : string.Empty;
    
    public bool IsReady() => Resources.Load(Constants.Resource.RESOURCE_LIST) != null;
    public bool IsNull() => false;
}

public record ResourcesOrder : ResourceOrder {
    
    public string assetName;
}

public record ResourcesUnloadAllOrder : ResourceSubOrder {

    public Action<AsyncOperation> callback;
}