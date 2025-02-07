using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

public static class DynamicMethodProvider {

    private static readonly Dictionary<Type, Dictionary<string, Func<object, object>>> _getValueFuncDic = new();

    public static Func<object, object> GetFieldValueFunc(object obj, string name) => _getValueFuncDic.TryGetValue(obj.GetType(), out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : obj.GetType().TryGetFieldInfo(out var info, name) ? GetFieldValueFunc(obj, info) : null;
    public static Func<object, object> GetFieldValueFunc(Type type, string name) => _getValueFuncDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(name, out var func) ? func : type.TryGetFieldInfo(out var info, name) ? GetFieldValueFunc(type, info) : null;

    public static Func<object, object> GetFieldValueFunc(object obj, FieldInfo info) => GetFieldValueFunc(obj.GetType(), info);
    
    public static Func<object, object> GetFieldValueFunc(Type type, FieldInfo info) {
        if (_getValueFuncDic.TryGetValue(type, out var funcDic) && funcDic.TryGetValue(info.Name, out var func)) {
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
        
        _getValueFuncDic.AutoAdd(type, info.Name, (Func<object, object>) dynamicMethod.CreateDelegate(typeof(Func<object, object>)));
        return _getValueFuncDic[type][info.Name];
    }

    // TODO. GetProperty 구현. PropertyInfo의 GetGetMethod를 Call 하여 반환 DynamicMethod Func 반환
}