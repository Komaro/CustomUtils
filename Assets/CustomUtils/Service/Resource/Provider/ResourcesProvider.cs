using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[ResourceProvider(105)]
[ResourceSubProvider(999)]
public class ResourcesProvider : IResourceProvider {
    
    private readonly Dictionary<string, string> _resourcePathDic = new();

    private bool _isLoaded = false;
    
    public bool Valid() => Resources.Load(Constants.Resource.RESOURCE_LIST) != null;

    public void Init() { }

    public void Load() {
        _resourcePathDic.Clear();
        var textAsset = Resources.Load<TextAsset>(Constants.Resource.RESOURCE_LIST);
        if (textAsset != null) {
            var jObject = JObject.Parse(textAsset.text);
            foreach (var pair in jObject) {
                if (pair.Value != null) {
                    _resourcePathDic.AutoAdd(pair.Key, pair.Value.ToString());
                }
            }
        }

        _isLoaded = true;
    }
    
    public void Unload(Dictionary<string, Object> cacheResource) => cacheResource.SafeClear(Resources.UnloadAsset);
    public Object Get(string name) => _resourcePathDic.TryGetValue(name.ToUpper(), out var path) ? Resources.Load(path) : null;
    public string GetPath(string name) => _resourcePathDic.TryGetValue(name.ToUpper(), out var path) ? path : string.Empty;
    public bool IsLoaded() => _isLoaded;
}
