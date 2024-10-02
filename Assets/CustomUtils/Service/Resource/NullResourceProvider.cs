using UnityEngine;
using Object = UnityEngine.Object;

public sealed class NullResourceProvider : IResourceProvider {

    public void Init() => Logger.TraceLog($"{nameof(Init)} {nameof(NullResourceProvider)}", Color.yellow);
    public void Clear() => Logger.TraceLog($"{nameof(Clear)} {nameof(NullResourceProvider)}", Color.yellow);

    public TOrder ExecuteOrder<TOrder>(TOrder order) where TOrder : ResourceOrder {
        Logger.TraceLog($"{nameof(ExecuteOrder)} {nameof(NullResourceProvider)}", Color.yellow);
        return order;
    }

    public void Load(ResourceOrder order) => Logger.TraceLog($"{nameof(Load)} {nameof(NullResourceProvider)}", Color.yellow);
    public void Unload(ResourceOrder order) => Logger.TraceLog($"{nameof(Unload)} {nameof(NullResourceProvider)}", Color.yellow);

    public Object Get(string name) {
        Logger.TraceLog($"{nameof(Get)} {nameof(NullResourceProvider)}", Color.yellow);
        return null;
    }

    public Object Get(ResourceOrder order) {
        Logger.TraceLog($"{nameof(Get)} {nameof(NullResourceProvider)}", Color.yellow);
        return null;
    }

    public string GetPath(string name) {
        Logger.TraceLog($"{nameof(GetPath)} {nameof(NullResourceProvider)}", Color.yellow);
        return string.Empty;
    }

    public bool IsNull() => true;
    public bool IsReady() => true;
}