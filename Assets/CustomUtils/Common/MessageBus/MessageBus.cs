using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IMessage { }

public interface IMessageHandler : IDisposable { }

public interface IMessageHandler<in T> : IMessageHandler where T : IMessage {

    public void Handle(T message);
}

public interface IAsyncMessageHandler<in TMessage> : IMessageHandler<TMessage>, IAsyncDisposable where TMessage : IMessage {

    public Task HandleAsync(TMessage message);
}

public static class MessageBus {

    private static Dictionary<Type, HashSet<IMessageHandler>> _handlerDic = new();

    public static void Subscribe<TMessage>(IMessageHandler<TMessage> handler) where TMessage : IMessage {
        var handlerSet = _handlerDic.GetOrAdd(typeof(TMessage), () => new HashSet<IMessageHandler>());
        if (handlerSet.Contains(handler) == false) {
            handlerSet.Add(handler);
        }
    }

    public static void Unsubscribe<TMessage>() where TMessage : IMessage => _handlerDic.AutoRemove(typeof(TMessage));

    public static void Unsubscribe<TMessage>(IMessageHandler<TMessage> handler) where TMessage : IMessage {
        if (_handlerDic.TryGetValue(typeof(TMessage), out var handlerSet)) {
            handlerSet.Remove(handler);
        }
    }

    public static void Publish<TMessage>(TMessage message) where TMessage : IMessage {
        if (_handlerDic.TryGetValue(typeof(TMessage), out var handlerSet)) {
            foreach (var handler in handlerSet) {
                (handler as IMessageHandler<TMessage>)?.Handle(message);
            }
        }
    }

    public static async Task PublishAsync<TMessage>(TMessage message) where TMessage : IMessage {
        if (_handlerDic.TryGetValue(typeof(TMessage), out var handlerSet)) {
            foreach (var handler in handlerSet) {
                if (handler is IAsyncMessageHandler<TMessage> asyncHandler) {
                    await asyncHandler.HandleAsync(message);
                }
            }
        }
    }

    public static bool Exists<TMessage>() where TMessage : IMessage => _handlerDic.TryGetValue(typeof(TMessage), out var handlerSet) && handlerSet.Any();
    public static bool Exists<TMessage>(IMessageHandler<TMessage> handler) where TMessage : IMessage => _handlerDic.TryGetValue(typeof(TMessage), out var handlerSet) && handlerSet.Contains(handler);
}