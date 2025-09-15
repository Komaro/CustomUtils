using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionProvider {

    public static Func<TEnum, int> GetEnumToIntFun<TEnum>() where TEnum : struct, Enum => StaticEnumExpressionCache<TEnum>.EnumToIntFunc;
    public static Func<int, TEnum> GetIntToEnumFunc<TEnum>() where TEnum : struct, Enum => StaticEnumExpressionCache<TEnum>.IntToEnumFunc;
    
    private static class StaticEnumExpressionCache<TEnum> where TEnum : struct, Enum {
        
        internal static readonly Func<TEnum, int> EnumToIntFunc;
        internal static readonly Func<int, TEnum> IntToEnumFunc;

        static StaticEnumExpressionCache() {
            var parameter = Expression.Parameter(typeof(TEnum));
            var body = Expression.Convert(parameter, typeof(int));
            var func = Expression.Lambda<Func<TEnum, int>>(body, parameter);
            EnumToIntFunc = func.Compile();
            
            parameter = Expression.Parameter(typeof(int));
            body = Expression.Convert(parameter, typeof(TEnum));
            IntToEnumFunc = Expression.Lambda<Func<int, TEnum>>(body, parameter).Compile();
        }
    }

    public static bool TryGetMappingExpression<T>(string methodName, out Expression<Func<T, object>> expression) => (expression = GetMappingExpression<T>(methodName)) != null;
    public static Expression<Func<T, object>> GetMappingExpression<T>(string methodName) => StaticMappingExpressionCache<T>.MappingFuncDic.TryGetValue(methodName, out var expression) ? expression : null;

    private static class StaticMappingExpressionCache<T> {
        
        internal static readonly Dictionary<string, Expression<Func<T, object>>> MappingFuncDic = new();
        
        static StaticMappingExpressionCache() {
            var parameter = Expression.Parameter(typeof(T));
            foreach (var info in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                var fieldType = info.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(NotifyProperty<>)) {
                    var notifyFieldProperty = Expression.PropertyOrField(parameter, info.Name);
                    var valueProperty = Expression.PropertyOrField(notifyFieldProperty, nameof(NotifyProperty<T>.Value));
                    var access = Expression.MakeMemberAccess(notifyFieldProperty, valueProperty.Member);
                    var convert = Expression.Convert(access, typeof(object));
                    MappingFuncDic.TryAdd(info.Name, Expression.Lambda<Func<T, object>>(convert, parameter));
                }
            }
        }
    }

    private static readonly MultiLevelDictionary<Type, string, Func<object, object>> _getFieldValueFuncDic = new();

    public static Func<object, object> GetFieldValueFunc(object obj, string name) => obj.GetType().TryGetFieldInfo(out var info, name) ? GetFieldValueFunc(info, obj) : null;

    public static Func<object, object> GetFieldValueFunc(FieldInfo info, object obj) {
        if (_getFieldValueFuncDic.TryGetValue(obj.GetType(), info.Name, out var func) == false) {
            var parameter = Expression.Parameter(typeof(object), nameof(obj));
            var body = Expression.Convert(Expression.Field(Expression.Convert(parameter, obj.GetType()), info), typeof(object));
            func = Expression.Lambda<Func<object, object>>(body, parameter).Compile();
            _getFieldValueFuncDic.Add(obj.GetType(), info.Name, func);
        }

        return func;
    }

    #region [Constructor]
    
    public static bool TryCreateConstructorFunc<TResult>(out Func<TResult> func) => (func = CreateConstructorFunc<TResult>()) != null;
    public static Func<TResult> CreateConstructorFunc<TResult>() => CreateConstructorDelegate(typeof(TResult)) as Func<TResult>;
    
    public static bool TryCreateConstructorFunc<T, TResult>(out Func<T, TResult> func) => (func = CreateConstructorFunc<T, TResult>()) != null;
    public static Func<T, TResult> CreateConstructorFunc<T, TResult>() => CreateConstructorDelegate(typeof(TResult), typeof(T)) as Func<T, TResult>;

    public static bool TryCreateConstructorFunc<T1, T2, TResult>(out Func<T1, T2, TResult> func) => (func = CreateConstructorFunc<T1, T2, TResult>()) != null;
    public static Func<T1, T2, TReturn> CreateConstructorFunc<T1, T2, TReturn>() => CreateConstructorDelegate(typeof(TReturn), typeof(T1), typeof(T2)) as Func<T1, T2, TReturn>;
    
    public static bool TryCreateConstructorFunc<TResult>(Type type, out Func<TResult> func) => (func = CreateConstructorFunc<TResult>(type)) != null;
    public static Func<TResult> CreateConstructorFunc<TResult>(Type type) => CreateConstructorDelegate(type) as Func<TResult>;
    
    public static bool TryCreateConstructorFunc<T, TResult>(Type type, out Func<T, TResult> func) => (func = CreateConstructorFunc<T, TResult>(type)) != null;
    public static Func<T, TResult> CreateConstructorFunc<T, TResult>(Type type) => CreateConstructorDelegate(type, typeof(T)) as Func<T, TResult>;

    public static bool TryCreateConstructorFunc<T1, T2, TResult>(Type type, out Func<T1, T2, TResult> func) => (func = CreateConstructorFunc<T1, T2, TResult>(type)) != null;
    public static Func<T1, T2, TReturn> CreateConstructorFunc<T1, T2, TReturn>(Type type) => CreateConstructorDelegate(type, typeof(TReturn), typeof(T1), typeof(T2)) as Func<T1, T2, TReturn>;
    
    public static bool TryCreateConstructorFunc<T>(out T func, Type type, params Type[] paramTypes) where T : Delegate => (func = CreateConstructorFunc<T>(type, paramTypes)) != null;
    public static T CreateConstructorFunc<T>(Type type, params Type[] paramTypes) where T : Delegate => CreateConstructorDelegate(type, paramTypes) as T;
    
    public static bool TryCreateConstructorDelegate(out Delegate outDelegate, Type type, params Type[] paramTypes) => (outDelegate = CreateConstructorDelegate(type, paramTypes)) != null;

    public static Delegate CreateConstructorDelegate(Type type, params Type[] paramTypes) {
        var constructorType = type.GetConstructor(paramTypes);
        if (constructorType != null) {
            var parameters = constructorType.GetParameters().Select(info => Expression.Parameter(info.ParameterType, info.Name)).ToList();
            var body = Expression.New(constructorType, parameters);
            return Expression.Lambda(body, parameters).Compile();
        }

        Logger.TraceError($"Could not find a constructor in the {type.GetCleanFullName()} that matches the {nameof(paramTypes)} [{paramTypes.ToStringCollection(paramType => paramType.GetCleanFullName(), ", ")}]");
        return null;
    }
    
    #endregion
}
