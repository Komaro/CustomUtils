using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class TcpStructHandlerProvider {

    private static readonly Dictionary<Enum, Type> _handlerEnumDic = new();
    private static readonly Dictionary<Type, Type> _handlerGenericDic = new();
    private static readonly ConcurrentDictionary<Type, ITcpHandler> _handlerDic = new();

    static TcpStructHandlerProvider() {
        foreach (var type in ReflectionProvider.GetInterfaceTypes<ITcpHandler>().ToHashSetWithDistinct()) {
            if (type.IsAbstract || type.IsInterface) {
                continue;
            }

            var genericType = type.BaseType?.GetGenericArguments().FirstOrDefault();
            if (genericType != null && _handlerGenericDic.TryAdd(genericType, type) == false) {
                Logger.TraceLog($"Duplicate Generic type || {genericType}", Color.red);
            }

            if (type.TryGetCustomAttribute<TcpHandlerAttribute>(out var attribute) && _handlerEnumDic.TryAdd(attribute.type, type) == false) {
                Logger.TraceLog($"Duplicate {nameof(Enum)} type || {attribute.type}", Color.red);
            }
        }
    }

    public static bool TryGetReceiveHandler(TCP_BODY bodyType, out ITcpReceiveHandler handler) => (handler = GetReceiveHandler(bodyType)) != null;

    public static ITcpReceiveHandler GetReceiveHandler(TCP_BODY bodyType) {
        try {
            if (_handlerEnumDic.TryGetValue(bodyType, out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpReceiveHandler;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryGetReceiveHandler<T>(out ITcpReceiveHandler handler) where T : ITcpPacket => (handler = GetReceiveHandler<T>()) != null;

    public static ITcpReceiveHandler GetReceiveHandler<T>() where T : ITcpPacket {
        try {
            if (_handlerGenericDic.TryGetValue(typeof(T), out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpReceiveHandler;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return default;
    }

    public static bool TryGetSendHandler<T>(TCP_BODY bodyType, out ITcpSendHandler<T> handler) where T : ITcpPacket => (handler = GetSendHandler<T>(bodyType)) != null;

    public static ITcpSendHandler<T> GetSendHandler<T>(TCP_BODY bodyType) where T : ITcpPacket {
        try {
            if (_handlerEnumDic.TryGetValue(bodyType, out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpSendHandler<T>;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryGetSendHandler<T>(out ITcpSendHandler<T> handler) where T : ITcpPacket => (handler = GetSendHandler<T>()) != null;

    public static ITcpSendHandler<T> GetSendHandler<T>() where T : ITcpPacket {
        try {
            if (_handlerGenericDic.TryGetValue(typeof(T), out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpSendHandler<T>;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return null;
    }

    public static TcpHeader CreateErrorHeader(TcpSession session, TCP_ERROR error) => new(session, TCP_BODY.ERROR);

}

public class TcpHandlerAttribute : Attribute {
    
    public readonly Enum type;

    public TcpHandlerAttribute(object type) {
        if (type is Enum enumType) {
            this.type = enumType;
        }
    }
}