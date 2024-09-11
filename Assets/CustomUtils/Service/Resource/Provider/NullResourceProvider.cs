using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class NullResourceProvider : IResourceProvider {

    public NullResourceProvider() => Logger.TraceLog($"Temporarily create {nameof(NullResourceProvider)}", Color.red);
    public bool Valid() => false;
    public void Init() { }
    public void Load(ResourceProviderOrder order) {
        throw new System.NotImplementedException();
    }

    public void Unload() {
        throw new System.NotImplementedException();
    }

    public void Unload(ResourceProviderOrder order) {
        throw new System.NotImplementedException();
    }

    public void Load() { }
    public void Unload(IDictionary<string, Object> cacheResource) { }
    public Object Get(string name) => null;
    public string GetPath(string name) => string.Empty;
    public bool IsLoaded() => false;
    public bool IsNull() => true;
}