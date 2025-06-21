using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

public class MessageBusTestRunner {

    [Test]
    public void MessageBusTest() {
        var message_01 = new SampleMessage("Message_01");
        var message_02 = new SampleMessage("Message_02");
        var message_03 = new SampleMessage("Message_03");
        var message_04 = new SampleMessage("Message_04");

        var handler = new SampleMessageHandler();
        MessageBus.Subscribe(handler);
        
        MessageBus.Publish(message_01);
        MessageBus.Publish(message_02);
        MessageBus.Publish(message_03);
        MessageBus.Publish(message_04);

        handler.Dispose();
        Assert.IsFalse(MessageBus.Exists<SampleMessage>());
        Assert.IsFalse(MessageBus.Exists(handler));
    }

    [Test]
    public async Task MessageBusAsyncTest() {
        var message_01 = new SampleMessage("Message_01");
        var message_02 = new SampleMessage("Message_02");
        var message_03 = new SampleMessage("Message_03");
        var message_04 = new SampleMessage("Message_04");

        var handler_01 = new SampleMessageHandler();
        MessageBus.Subscribe(handler_01);

        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));

        var handler_02 = new SampleMessageHandler_02();
        MessageBus.Subscribe(handler_02);
        
        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));
        
        MessageBus.Unsubscribe(handler_02);
        
        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));
        
        await Task.WhenAll(handler_01.DisposeAsync().AsTask(), handler_02.DisposeAsync().AsTask());
        
        Assert.IsFalse(MessageBus.Exists<SampleMessage>());
        Assert.IsFalse(MessageBus.Exists(handler_01));
        Assert.IsFalse(MessageBus.Exists(handler_02));
    }
}

public class SampleMessage : IMessage {

    public string message;

    public SampleMessage(string message) => this.message = message;
}

public class SampleMessageHandler : IAsyncMessageHandler<SampleMessage> {

    public void Handle(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
    }

    public Task HandleAsync(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
        return Task.CompletedTask;
    }
    
    public void Dispose() => MessageBus.Unsubscribe(this);

    public async ValueTask DisposeAsync() {
        Dispose();
        await Task.CompletedTask;
    }
}

public class SampleMessageHandler_02 : IAsyncMessageHandler<SampleMessage> {
    ~SampleMessageHandler_02() => Dispose();

    public void Handle(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
    }

    public Task HandleAsync(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
        return Task.CompletedTask;
    }
    
    public WeakReference<SampleMessageHandler_02> DisposeCheck() {
        Dispose();
        return new WeakReference<SampleMessageHandler_02>(this);
    }

    public void Dispose() => MessageBus.Unsubscribe(this);

    public async ValueTask DisposeAsync() {
        Dispose();
        await Task.CompletedTask;
    }
}