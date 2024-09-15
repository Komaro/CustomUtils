using UnityEngine;

public interface IResourceModule {
    
    void Init();
    void ExecuteOrder(ResourceOrder order);
    void Load(ResourceOrder order);
    void Unload(ResourceOrder order);
    void Clear();
    Object Get(string name);
    Object Get(ResourceOrder order);
    string GetPath(string name);
    bool IsReady();
}