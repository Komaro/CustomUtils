using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Codice.CM.Common;

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
    public static Expression<Func<T, object>> GetMappingExpression<T>(string methodName) => StaticMappingExpressionCache<T>.MappingDic.TryGetValue(methodName, out var expression) ? expression : null;

    private static class StaticMappingExpressionCache<T> {
        
        internal static readonly Dictionary<string, Expression<Func<T, object>>> MappingDic = new();
        
        static StaticMappingExpressionCache() {
            var parameter = Expression.Parameter(typeof(T));
            foreach (var info in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                var fieldType = info.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(NotifyProperty<>)) {
                    var notifyFieldProperty = Expression.PropertyOrField(parameter, info.Name);
                    var valueProperty = Expression.PropertyOrField(notifyFieldProperty, nameof(NotifyProperty<T>.Value));
                    var access = Expression.MakeMemberAccess(notifyFieldProperty, valueProperty.Member);
                    var convert = Expression.Convert(access, typeof(object));
                    MappingDic.TryAdd(info.Name, Expression.Lambda<Func<T, object>>(convert, parameter));
                }
            }
        }
    }

    private static Dictionary<Type, Dictionary<string, Func<object, object>>> _getFieldValueDic = new();

    public static Func<object, object> GetFieldValueFunc(object obj, string name) => obj.GetType().TryGetFieldInfo(out var info, name) ? GetFieldValueFunc(info, obj) : null;

    public static Func<object, object> GetFieldValueFunc(FieldInfo info, object obj) {
        if (_getFieldValueDic.TryGetValue(obj.GetType(), out var funcDic) == false || funcDic.TryGetValue(info.Name, out var func) == false) {
            var parameter = Expression.Parameter(typeof(object), "TestClass");
            var body = Expression.Convert(Expression.Field(Expression.Convert(parameter, obj.GetType()), info), typeof(object));
            func = Expression.Lambda<Func<object, object>>(body, parameter).Compile();
            _getFieldValueDic.AutoAdd(obj.GetType(), info.Name, func);
        }

        return func;
    }
}
