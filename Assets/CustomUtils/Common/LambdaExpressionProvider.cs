

using System;
using System.Linq.Expressions;

public static class LambdaExpressionProvider {
    
    public static Func<TEnum, int> GetEnumToIntFun<TEnum>() where TEnum : struct, Enum => StaticLambdaCache<TEnum>.EnumToIntFunc;
    public static Func<int, TEnum> GetIntToEnumFunc<TEnum>() where TEnum : struct, Enum => StaticLambdaCache<TEnum>.IntToEnumFunc;
    // public static Func<string, TEnum> GetStringToEnumFunc<TEnum>() where TEnum : struct, Enum => StaticLambdaCache<TEnum>.StringToEnumFunc;

    private static class StaticLambdaCache<TEnum> where TEnum : struct, Enum {
        
        public static Func<TEnum, int> EnumToIntFunc;
        public static Func<int, TEnum> IntToEnumFunc;
        public static Func<string, TEnum> StringToEnumFunc;

        static StaticLambdaCache() {
            var parameter = Expression.Parameter(typeof(TEnum));
            var body = Expression.Convert(parameter, typeof(int));
            var func = Expression.Lambda<Func<TEnum, int>>(body, parameter);
            EnumToIntFunc = func.Compile();
            
            parameter = Expression.Parameter(typeof(int));
            body = Expression.Convert(parameter, typeof(TEnum));
            IntToEnumFunc = Expression.Lambda<Func<int, TEnum>>(body, parameter).Compile();
        }
    }
}
