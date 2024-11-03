using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

public static class LambdaExpressionProvider {

    public static Func<TEnum, int> GetEnumToIntFun<TEnum>() where TEnum : struct, Enum => StaticEnumLambdaCache<TEnum>.EnumToIntFunc;
    public static Func<int, TEnum> GetIntToEnumFunc<TEnum>() where TEnum : struct, Enum => StaticEnumLambdaCache<TEnum>.IntToEnumFunc;
    
    private static class StaticEnumLambdaCache<TEnum> where TEnum : struct, Enum {
        
        public static readonly Func<TEnum, int> EnumToIntFunc;
        public static readonly Func<int, TEnum> IntToEnumFunc;

        static StaticEnumLambdaCache() {
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

    public static Expression<Func<T, object>> GetMappingExpression<T>(string methodName) {
        if (StaticMappingExpressionCache<T>.MappingDic.TryGetValue(methodName, out var expression)) {
            return expression;
        }

        return null;
    }

    private static class StaticMappingExpressionCache<T> {
        
        public static readonly Dictionary<string, Expression<Func<T, object>>> MappingDic = new();
        
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
}
