using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[ResourceProviderOrder(5)]
public class ResourcesProvider : IResourceProvider {
    
    private readonly Dictionary<string, string> _resourcePathDic = new();

    private bool _isLoaded = false;
    
    public static bool Valid() => Resources.Load(Constants.Resource.RESOURCE_LIST_JSON) != null;

    public void Init() { }

    public void Load() {
        _resourcePathDic.Clear();
        var textAsset = Resources.Load<TextAsset>(Constants.Resource.RESOURCE_LIST_JSON);
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
    public bool IsLoaded() => _isLoaded;
}
