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
    private static readonly ConcurrentDictionary<Type, ITcpStructHandler> _handlerDic = new();

    static TcpStructHandlerProvider() {
        foreach (var type in ReflectionProvider.GetInterfaceTypes<ITcpStructHandler>().ToHashSetWithDistinct()) {
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

    public static bool TryGetReceiveHandler(TCP_STRUCT_BODY bodyType, out ITcpStructReceiveHandler handler) => (handler = GetReceiveHandler(bodyType)) != null;

    public static ITcpStructReceiveHandler GetReceiveHandler(TCP_STRUCT_BODY bodyType) {
        try {
            if (_handlerEnumDic.TryGetValue(bodyType, out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpStructHandler) as ITcpStructReceiveHandler;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryGetReceiveHandler<T>(out ITcpStructReceiveHandler handler) where T : ITcpPacket => (handler = GetReceiveHandler<T>()) != null;

    public static ITcpStructReceiveHandler GetReceiveHandler<T>() where T : ITcpPacket {
        try {
            if (_handlerGenericDic.TryGetValue(typeof(T), out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpStructHandler) as ITcpStructReceiveHandler;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return default;
    }

    public static bool TryGetSendHandler<T>(TCP_STRUCT_BODY bodyType, out ITcpStructSendHandler<T> handler) where T : ITcpPacket => (handler = GetSendHandler<T>(bodyType)) != null;

    public static ITcpStructSendHandler<T> GetSendHandler<T>(TCP_STRUCT_BODY bodyType) where T : ITcpPacket {
        try {
            if (_handlerEnumDic.TryGetValue(bodyType, out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpStructHandler) as ITcpStructSendHandler<T>;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryGetSendHandler<T>(out ITcpStructSendHandler<T> handler) where T : ITcpPacket => (handler = GetSendHandler<T>()) != null;

    public static ITcpStructSendHandler<T> GetSendHandler<T>() where T : ITcpPacket {
        try {
            if (_handlerGenericDic.TryGetValue(typeof(T), out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpStructHandler) as ITcpStructSendHandler<T>;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return null;
    }

    public static TcpHeader CreateErrorHeader(TcpSession session, TCP_ERROR error) => new(session, TCP_STRUCT_BODY.ERROR);

    public static async Task<Exception> ResponseExceptionAsync<TPacket>(_TcpResponseException<TPacket> exception, TcpSession session, CancellationToken token) where TPacket : ITcpPacket {
        if (session.Connected) {
            var packet = exception.GetPacketBytes(session);
            await session.Stream.WriteAsync(packet, token);
            return exception;
        }

        return new DisconnectSessionException(session);
    }
    
    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, TcpSession session, CancellationToken token) where T : struct, ITcpPacket {
        if (session.Connected) {
            var header = exception.CreateErrorHeader(session.ID);
            await session.Stream.WriteAsync(header.ToBytes(), token);
            return exception;
        }

        return new DisconnectSessionException(session);
    }
    
    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, TcpClient client, CancellationToken token) where T : struct, ITcpPacket {
        if (client.Connected) {
            var header = exception.CreateErrorHeader();
            await client.GetStream().WriteAsync(header.ToBytes(), token);
            return exception;
        }
        
        return new DisconnectSessionException(client);
    }
    
    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, Stream stream, CancellationToken token) where T : struct, ITcpPacket {
        if (stream.CanWrite) {
            var header = exception.CreateErrorHeader();
            await stream.WriteAsync(header.ToBytes(), token);
            return exception;
        }
        
        return new InvalidOperationException($"Current {nameof(stream)} is not writable");
    }
}

public class TcpHandlerAttribute : Attribute {
    
    public readonly Enum type;

    public TcpHandlerAttribute(object type) {
        if (type is Enum enumType) {
            this.type = enumType;
        }
    }
}