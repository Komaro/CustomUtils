using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class GameDBProvider : IImplementNullable {
    
    public abstract bool Init(IEnumerable<Type> dbTypes);
    public abstract IEnumerable<TData> GetData<TData>();
    public abstract void Clear();
    
    public bool IsNull() => this is NullGameDBProvider;
}

[Priority(999999)]
public class NullGameDBProvider : GameDBProvider {
    
    public NullGameDBProvider() => Logger.TraceLog($"Temporarily create {nameof(NullGameDBProvider)}", Color.red);
    public override bool Init(IEnumerable<Type> dbTypes) => true;
    public override IEnumerable<TData> GetData<TData>() => Enumerable.Empty<TData>();
    public override void Clear() { }
}