using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class TcpHandlerProvider {

    private static readonly Dictionary<Enum, Type> _handlerEnumDic = new();
    private static readonly Dictionary<Type, Type> _handlerGenericDic = new();
    private static readonly ConcurrentDictionary<Type, ITcpHandler> _handlerDic = new();

    static TcpHandlerProvider() {
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

    public static bool TryGetReceiveHandler<T>(out ITcpReceiveHandler handler) where T : struct, ITcpStructure => (handler = GetReceiveHandler<T>()) != null;

    public static ITcpReceiveHandler GetReceiveHandler<T>() where T : struct, ITcpStructure {
        try {
            if (_handlerGenericDic.TryGetValue(typeof(T), out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpReceiveHandler;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return default;
    }

    public static bool TryGetSendHandler<T>(TCP_BODY bodyType, out ITcpSendHandler<T> handler) where T : struct, ITcpStructure => (handler = GetSendHandler<T>(bodyType)) != null;

    public static ITcpSendHandler<T> GetSendHandler<T>(TCP_BODY bodyType) where T : struct, ITcpStructure {
        try {
            if (_handlerEnumDic.TryGetValue(bodyType, out var type)) {
                return _handlerDic.GetOrAdd(type, _ => Activator.CreateInstance(type) as ITcpHandler) as ITcpSendHandler<T>;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static bool TryGetSendHandler<T>(out ITcpSendHandler<T> handler) where T : struct, ITcpStructure => (handler = GetSendHandler<T>()) != null;

    public static ITcpSendHandler<T> GetSendHandler<T>() where T : struct, ITcpStructure {
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

    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, TcpSession session, CancellationToken token) where T : struct, ITcpStructure {
        if (session.Connected) {
            var header = exception.CreateErrorHeader(session.ID);
            await session.Stream.WriteAsync(header.ToBytes(), token);
            return exception;
        }

        return new DisconnectSessionException(session);
    }
    
    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, TcpClient client, CancellationToken token) where T : struct, ITcpStructure {
        if (client.Connected) {
            var header = exception.CreateErrorHeader();
            await client.GetStream().WriteAsync(header.ToBytes(), token);
            return exception;
        }
        
        return new DisconnectSessionException(client);
    }
    
    public static async Task<Exception> AsyncResponseException<T>(TcpResponseException<T> exception, Stream stream, CancellationToken token) where T : struct, ITcpStructure {
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

public interface ITcpHandler {

}

public interface ITcpReceiveHandler : ITcpHandler {
    
    public Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
}

public interface ITcpSendHandler<in T> : ITcpHandler where T : struct, ITcpStructure {
    
    public Task SendAsync(TcpSession session, T send, CancellationToken token);
}

[RequiresAttributeImplementation(typeof(TcpHandlerAttribute))]
public abstract class TcpHandler<T> : ITcpReceiveHandler, ITcpSendHandler<T> where T : struct, ITcpStructure {

    public abstract Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token);
    public abstract Task SendAsync(TcpSession session, T send, CancellationToken token);

    public virtual TcpHeader CreateHeader(TcpSession session, TCP_BODY bodyType, ref T structure) => new() {
        sessionId = session.ID,
        byteLength = Marshal.SizeOf<T>(),
        bodyType = bodyType
    };

    protected async Task WriteAsyncWithCancellationCheck(NetworkStream stream, byte[] bytes, CancellationToken token) {
        await stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }
    
    protected async Task WriteAsyncWithCancellationCheck(TcpSession session, byte[] bytes, CancellationToken token) {
        await session.Stream.WriteAsync(bytes, token);
        token.ThrowIfCancellationRequested();
    }

    protected bool TryGetStruct(byte[] bytes, out T structure) {
        var outStructure = bytes.ToStruct<T>();
        if (outStructure.HasValue) {
            structure = outStructure.Value;
            return true;
        }

        structure = default;
        return false;
    }

    public ITcpReceiveHandler GetReceiveHandler() => this;
}

[TcpHandler(TCP_BODY.TEST)]
public class RequestTestHandler : TcpHandler<TcpRequestTest> {

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        if (TryGetStruct(bytes, out var request) && request.IsValid()) {
            if (request.count <= 0) {
                throw await TcpHandlerProvider.AsyncResponseException(new InvalidTestCount(), session, token);
            }

            if (TcpHandlerProvider.TryGetSendHandler<TcpResponseTest>(out var handler)) {
                await handler.SendAsync(session, new TcpResponseTest($"Count : {request.count}"), token);
            }
        }
    }

    public override async Task SendAsync(TcpSession session, TcpRequestTest send, CancellationToken token) {
        if (session.Connected && send.IsValid()) {
            var header = CreateHeader(session, TCP_BODY.TEST, ref send);
            await session.Stream.WriteAsync(header.ToBytes(), token);
            await session.Stream.WriteAsync(send.ToBytes(), token);
        }
    }
}

[TcpHandler(TCP_BODY.TEST_STRING)]
public class ResponseTestHandler : TcpHandler<TcpResponseTest> {

    public override TcpHeader CreateHeader(TcpSession session, TCP_BODY bodyType, ref TcpResponseTest structure) => new(session, bodyType, structure.text.GetByteCount());

    public override async Task ReceiveAsync(TcpSession session, byte[] bytes, CancellationToken token) {
        var text = bytes.GetString();
        Logger.TraceLog(text);
        await Task.CompletedTask;
    }

    public override async Task SendAsync(TcpSession session, TcpResponseTest send, CancellationToken token) {
        if (session.Connected && send.IsValid()) {
            var header = CreateHeader(session, TCP_BODY.TEST_STRING, ref send);
            await WriteAsyncWithCancellationCheck(session, header.ToBytes(), token);
            await WriteAsyncWithCancellationCheck(session, send.text.ToBytes(), token);
        }
    }
}

