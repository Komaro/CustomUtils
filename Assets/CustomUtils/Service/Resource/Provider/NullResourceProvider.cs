using System.Collections.Generic;
using UnityEngine;

public class NullResourceProvider : IResourceProvider {

    public NullResourceProvider() => Logger.TraceLog($"Temporarily create {nameof(NullResourceProvider)}", Color.red);
    public bool Valid() => false;
    public void Init() { }
    public void Load() { }
    public void LoadAsync() { }
    public void Unload(Dictionary<string, Object> cacheResource) { }
    public Object Get(string name) => null;
    public string GetPath(string name) => string.Empty;
    public bool IsLoaded() => false;
}