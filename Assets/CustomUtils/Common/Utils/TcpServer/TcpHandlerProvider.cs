using System;
using System.Collections.Concurrent;
using System.Linq;

public class TcpHandlerProvider<TEnum> where TEnum : struct, Enum {

    private readonly ConcurrentDictionary<TEnum, Type> _handlerTypeDic = new();
    private readonly ConcurrentDictionary<Type, TEnum> _handlerGenericDic = new();
    
    private readonly ConcurrentDictionary<TEnum, ITcpHandler> _handlerDic = new();
    
    public TcpHandlerProvider(Type handlerType) {
        var handlerGenericType = handlerType.GetGenericTypeDefinition();
        foreach (var type in ReflectionProvider.GetInterfaceTypes<ITcpHandler>()) {
            if (type.IsAbstract || type.IsInterface) {
                continue;
            }
            
            var baseType = type.BaseType;
            for (; baseType is { IsGenericType: true }; baseType = baseType.BaseType) {
                if (baseType.GetGenericTypeDefinition() == handlerGenericType) {
                    var genericType = baseType.GetGenericArguments().FirstOrDefault();
                    if (genericType != null && type.TryGetCustomAttribute<TcpHandlerAttribute>(out var attribute) && attribute.body is TEnum enumType) {
                        _handlerTypeDic.TryAdd(enumType, type);
                        _handlerGenericDic.TryAdd(genericType, enumType);
                    }
                }
            }
        }
    }
    
    #region [Handler]
    
    public bool TryGetHandler(Type type, out ITcpHandler handler) => (handler = GetHandler(type)) != null;

    public ITcpHandler GetHandler(Type type) {
        if (_handlerGenericDic.TryGetValue(type, out var enumValue)) {
            if (TryGetHandler(enumValue, out var handler)) {
                return handler;
            }
            
            Logger.TraceError($"{enumValue} is an invalid enum value");
            return null;
        }

        Logger.TraceError($"{type.Name} is invalid handler generic type");
        return null;
    }
    
    public bool TryGetHandler(int body, out ITcpHandler handler) => (handler = GetHandler(body)) != null;
    public ITcpHandler GetHandler(int body) => GetHandler(EnumUtil.ConvertFast<TEnum>(body));
    
    public bool TryGetHandler(TEnum enumValue, out ITcpHandler handler) {
        handler = GetHandler(enumValue);
        return handler != null;
    }

    public bool TryGetHandler<TData>(out ITcpHandler handler) => (handler = GetHandler<TData>()) != null;

    public virtual ITcpHandler GetHandler<TData>() {
        if (_handlerGenericDic.TryGetValue(typeof(TData), out var enumValue)) {
            if (TryGetHandler(enumValue, out var handler)) {
                return handler;
            }

            Logger.TraceError($"{enumValue} is an invalid enum value");
            return null;
        }

        Logger.TraceError($"{typeof(TData).Name} is invalid handler generic type");
        return null;
    }

    public virtual ITcpHandler GetHandler(TEnum enumValue) {
        if (_handlerDic.TryGetValue(enumValue, out var handler)) {
            return handler;
        }
        
        if (_handlerTypeDic.TryGetValue(enumValue, out var handlerType) && SystemUtil.TryCreateInstance(out handler, handlerType)) {
            _handlerDic.TryAdd(enumValue, handler);
            return handler;
        } 
        
        Logger.TraceError($"{enumValue} is an invalid enum value");
        return null;
    }
    
    #endregion
    
    #region [Receive Handler]
    
    public bool TryGetReceiveHandler<TData>(int body, out ITcpReceiveHandler<TData> handler) => (handler = GetReceiveHandler<TData>(body)) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>(int body) => GetHandler(EnumUtil.ConvertFast<TEnum>(body)) as ITcpReceiveHandler<TData>;
    
    public bool TryGetReceiveHandler<TData>(TEnum body, out ITcpReceiveHandler<TData> handler) => (handler = GetReceiveHandler<TData>(body)) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>(TEnum body) => GetHandler(body) as ITcpReceiveHandler<TData>;

    public bool TryGetReceiveHandler<TData>(out ITcpReceiveHandler<TData> handler) => (handler = GetReceiveHandler<TData>()) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>() => TryGetHandler<TData>(out var handler) ? handler as ITcpReceiveHandler<TData> : null;

    #endregion

    #region [Send Handler]
    
    public bool TryGetSendHandler<TData>(TEnum body, out ITcpSendHandler<TData> handler) => (handler = GetSendHandler<TData>(body)) != null;
    public ITcpSendHandler<TData> GetSendHandler<TData>(TEnum body) => GetHandler(body) as ITcpSendHandler<TData>;

    public bool TryGetSendHandler<TData>(out ITcpSendHandler<TData> handler) => (handler = GetSendHandler<TData>()) != null;
    public ITcpSendHandler<TData> GetSendHandler<TData>() => GetHandler<TData>() as ITcpSendHandler<TData>;

    #endregion
}