using UnityEngine;

[ResourceProvider(10)]
public class AddressableProvider : IResourceProvider {

    public bool IsNull() => throw new System.NotImplementedException();

    public void Init() {
        throw new System.NotImplementedException();
    }

    public void Clear() {
        throw new System.NotImplementedException();
    }

    public TOrder ExecuteOrder<TOrder>(TOrder order) where TOrder : ResourceOrder => throw new System.NotImplementedException();

    public void Load(ResourceOrder order) {
        throw new System.NotImplementedException();
    }

    public void Unload(ResourceOrder order) {
        throw new System.NotImplementedException();
    }

    public Object Get(string name) => throw new System.NotImplementedException();

    public Object Get(ResourceOrder order) => throw new System.NotImplementedException();

    public string GetPath(string name) => throw new System.NotImplementedException();

    public bool IsReady() => throw new System.NotImplementedException();
}
