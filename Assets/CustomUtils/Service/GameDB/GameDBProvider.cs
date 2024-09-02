using System;
using System.Collections.Generic;
using UnityEngine;

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class GameDBProvider : IImplementNullable {
    
    public abstract bool Init(IEnumerable<Type> dbTypes);
    public abstract List<TData> GetDataList<TData>();
    public abstract void Clear();
    
    public virtual bool IsNull() => false;
}

[Priority(999999)]
public class NullGameDBProvider : GameDBProvider {
    
    public NullGameDBProvider() => Logger.TraceLog($"Temporarily create {nameof(NullGameDBProvider)}", Color.red);
    public override bool Init(IEnumerable<Type> dbTypes) => true;
    public override List<T> GetDataList<T>() => new();
    public override void Clear() { }
    public override bool IsNull() => true;
}