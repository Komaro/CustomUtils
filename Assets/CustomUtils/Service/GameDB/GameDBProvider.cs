using System;
using System.Collections.Generic;

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class GameDBProvider : IImplementNullable {
    
    public abstract void Init(IEnumerable<Type> dbTypes);
    public abstract List<T> GetList<T>();
    public abstract void Clear();
    public virtual bool IsNull() => false;
}

[Priority(999999)]
public class NullGameDBProvider : GameDBProvider {

    public override void Init(IEnumerable<Type> dbTypes) => Logger.TraceError($"{nameof(NullGameDBProvider)} initialized");
    public override List<T> GetList<T>() => new();
    public override void Clear() { }
    public override bool IsNull() => true;
}