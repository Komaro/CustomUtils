using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using UnityEngine;

// TODO. Sample
[RequiresAttributeImplementation(typeof(ScriptableObjectProviderAttribute))]
public interface IScriptableObjectProvider {

    public object Get<T>() => Get(typeof(T));
    public object Get(Type type);
}

// TODO. Sample
public class ScriptableObjectProviderAttribute : PriorityAttribute {
    
    public ScriptableObjectProviderAttribute(uint priority) : base(priority) { }
}

// TODO. Sample
[ScriptableObjectProvider(9999)]
public class AssetDatabaseProvider : IScriptableObjectProvider {

    public object Get(Type type) => throw new NotImplementedException();
}

// TODO. Sample
// TODO. 런타임에 ScriptableObject에 접근하기 위한 Interface 설계
public class ScriptableObjectService : IAsyncService {

    private readonly ImmutableArray<IScriptableObjectProvider> _providers;
    
    private readonly Dictionary<Type, object> _objectDic = new();
    
    Task IAsyncService.StartAsync() => throw new System.NotImplementedException();
    Task IAsyncService.StopAsync() => throw new System.NotImplementedException();

    public T Get<T>() where T : ScriptableObject {
        if (_objectDic.TryGetValue(typeof(T), out var obj) == false) {
            foreach (var provider in _providers) {
                obj = provider.Get<T>();
                if (obj != null) {
                    return obj as T;
                }
            }
        }

        return null;
    }
}
