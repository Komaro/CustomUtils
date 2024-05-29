using System.Collections.Generic;
using UnityEngine;

public class NullResourceProvider : IResourceProvider {

    public bool Valid() => false;
    public void Init() { }
    public void Load() { }
    public void Unload(Dictionary<string, Object> cacheResource) { }
    public Object Get(string name) => null;
    public string GetPath(string name) => string.Empty;
    public bool IsLoaded() => false;
}