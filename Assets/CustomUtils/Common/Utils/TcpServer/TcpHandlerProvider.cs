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
                        _handlerTypeDic.AutoAdd(enumType, type);
                        _handlerGenericDic.AutoAdd(genericType, enumType);
                    }
                }
            }
        }
    }
    
    #region [Handler]
    
    public bool TryGetHandler(int body, out ITcpHandler handler) => (handler = GetHandler(body)) != null;
    public ITcpHandler GetHandler(int body) => GetHandler(EnumUtil.ConvertFast<TEnum>(body));
    
    public bool TryGetHandler(TEnum enumValue, out ITcpHandler handler) {
        handler = GetHandler(enumValue);
        return handler != null;
    }
    
    public ITcpHandler GetHandler(TEnum enumValue) {
        if (_handlerDic.TryGetValue(enumValue, out var handler)) {
            return handler;
        }

        if (_handlerTypeDic.TryGetValue(enumValue, out var handlerType) && SystemUtil.TrySafeCreateInstance(handlerType, out handler)) {
            _handlerDic.TryAdd(enumValue, handler);
            return handler;
        } 
        
        Logger.TraceError($"{nameof(enumValue)} is an invalid enum value");
        return null;
    }

    public bool TryGetHandler<TData>(out ITcpHandler handler) where TData : ITcpPacket => (handler = GetHandler<TData>()) != null;

    public ITcpHandler GetHandler<TData>() where TData : ITcpPacket {
        if (_handlerGenericDic.TryGetValue(typeof(TData), out var enumValue) && TryGetHandler(enumValue, out var handler)) {
            return handler;
        }

        return null;
    }
    
    #endregion
    
    #region [Receive Handler]
    
    public bool TryGetReceiveHandler<TData>(int body, out ITcpReceiveHandler<TData> handler) where TData : ITcpPacket => (handler = GetReceiveHandler<TData>(body)) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>(int body) where TData : ITcpPacket => GetHandler(EnumUtil.ConvertFast<TEnum>(body)) as ITcpReceiveHandler<TData>;
    
    public bool TryGetReceiveHandler<TData>(TEnum body, out ITcpReceiveHandler<TData> handler) where TData : ITcpPacket => (handler = GetReceiveHandler<TData>(body)) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>(TEnum body) where TData : ITcpPacket => GetHandler(body) as ITcpReceiveHandler<TData>;

    public bool TryGetReceiveHandler<TData>(out ITcpReceiveHandler<TData> handler) where TData : ITcpPacket => (handler = GetReceiveHandler<TData>()) != null;
    public ITcpReceiveHandler<TData> GetReceiveHandler<TData>() where TData : ITcpPacket => TryGetHandler<TData>(out var handler) ? handler as ITcpReceiveHandler<TData> : null;

    #endregion

    #region [Send Handler]
    
    public bool TryGetSendHandler<TData>(TEnum body, out ITcpSendHandler<TData> handler) where TData : ITcpPacket => (handler = GetSendHandler<TData>(body)) != null;
    public ITcpSendHandler<TData> GetSendHandler<TData>(TEnum body) where TData : ITcpPacket => GetHandler(body) as ITcpSendHandler<TData>;

    public bool TryGetSendHandler<TData>(out ITcpSendHandler<TData> handler) where TData : ITcpPacket => (handler = GetSendHandler<TData>()) != null;
    public ITcpSendHandler<TData> GetSendHandler<TData>() where TData : ITcpPacket => GetHandler<TData>() as ITcpSendHandler<TData>;

    #endregion
}