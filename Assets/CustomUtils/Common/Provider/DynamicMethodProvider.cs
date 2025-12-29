using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

public static class DynamicMethodProvider {

    #region [Field]
    
    private static readonly Dictionary<Type, Dictionary<string, Func<object, object>>> _getFieldValueFuncDic = new();

    public static Func<object, object> GetFieldValueFunc(object obj, string name) => _getFieldValueFuncDic.TryGetValue(obj.GetType(), out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : obj.GetType().TryGetFieldInfo(name, out var info) ? GetFieldValueFunc(obj, info) : null;
    public static Func<object, object> GetFieldValueFunc(Type type, string name) => _getFieldValueFuncDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : type.TryGetFieldInfo(name, out var info) ? GetFieldValueFunc(type, info) : null;
    public static Func<object, object> GetFieldValueFunc(object obj, FieldInfo info) => GetFieldValueFunc(obj.GetType(), info);
    
    public static Func<object, object> GetFieldValueFunc(Type type, FieldInfo info) {
        if (_getFieldValueFuncDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(info.Name, out var func)) {
            return func;
        }

        return CreateFieldValueFunc(type, info);
    }

    private static Func<object, object> CreateFieldValueFunc(Type type, FieldInfo info) {
        var dynamicMethod = new DynamicMethod(info.Name, typeof(object), new[] { typeof(object) }, typeof(object));
        var generator = dynamicMethod.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, info);
        if (info.FieldType.IsValueType) {
            generator.Emit(OpCodes.Box, info.FieldType);
        }
        
        generator.Emit(OpCodes.Ret);
        
        _getFieldValueFuncDic.AutoAdd(type, info.Name, (Func<object, object>) dynamicMethod.CreateDelegate(typeof(Func<object, object>)));
        return _getFieldValueFuncDic[type][info.Name];
    }
    
    #endregion

    #region [Property]
    
    private static readonly MultiLevelDictionary<Type, string, Func<object, object>> _getPropertyValueDic = new();

    public static Func<object, object> GetPropertyValueFunc(object obj, string name) => _getPropertyValueDic.TryGetValue(obj.GetType(), out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : obj.GetType().TryGetPropertyInfo(name, out var info) ? GetPropertyValueFunc(obj, info) : null;
    public static Func<object, object> GetPropertyValueFunc(Type type, string name) => _getPropertyValueDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : type.TryGetPropertyInfo(name, out var info) ? GetPropertyValueFunc(type, info) : null;
    public static Func<object, object> GetPropertyValueFunc(object obj, PropertyInfo info) => GetPropertyValueFunc(obj.GetType(), info);

    public static Func<object, object> GetPropertyValueFunc(Type type, PropertyInfo info) {
        if (_getPropertyValueDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(info.Name, out var func)) {
            return func;
        }

        return CreatePropertyValueFunc(type, info);
    }

    private static Func<object, object> CreatePropertyValueFunc(Type type, PropertyInfo info) {
        if (info.TryGetGetGetMethod(out var methodInfo) == false) {
            Logger.TraceError($"Property {info.Name} does not have a getter implemented.");
            return null;
        }
        
        var dynamicMethod = new DynamicMethod(info.Name, typeof(object), new[] { typeof(object) }, typeof(object));
        var generator = dynamicMethod.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(methodInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt, methodInfo);

        if (info.PropertyType.IsValueType) {
            generator.Emit(OpCodes.Box, info.PropertyType);
        }
        
        generator.Emit(OpCodes.Ret);
        
        _getPropertyValueDic.Add(type, info.Name, (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>)));
        return _getPropertyValueDic[type, info.Name];
    }
    
    #endregion
}