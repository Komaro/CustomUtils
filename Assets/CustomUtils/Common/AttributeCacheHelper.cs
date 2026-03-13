using System;
using System.Collections.Generic;

public class AttributeCacheHelper<TAttribute> where TAttribute : Attribute {

    private readonly Dictionary<Type, TAttribute> _cachedAttributeDic = new();
    private readonly HashSet<Type> _cachedNonAttributeSet = new();

    public bool TryGet(Type type, out TAttribute attribute) => (attribute = Get(type)) != null;

    public TAttribute Get(Type type) {
        if (_cachedNonAttributeSet.Contains(type)) {
            return null;
        }

        
        if (_cachedAttributeDic.TryGetValue(type, out var attribute) == false) {
            if (type.TryGetCustomAttribute(out attribute)) {
                _cachedAttributeDic.Add(type, attribute);
            } else {
                _cachedNonAttributeSet.Add(type);
            }
        }
        
        return attribute;
    }

    public void Clear() {
        _cachedAttributeDic.Clear();
        _cachedNonAttributeSet.Clear();
    }
}