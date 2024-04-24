using UnityEngine;

[ResourceProviderOrder(100)]
public class NullResourceProvider : IResourceProvider {

    public static bool Valid() => false;
    public void Init() { }
    public void Load() { }
    public Object Get(string name) => null;
    public string GetPath(string name) => string.Empty;
    public bool IsLoaded() => false;
}