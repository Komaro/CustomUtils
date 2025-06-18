using System.Threading.Tasks;
using NUnit.Framework;

public class MessageBusTestRunner {

    [Test]
    public void MessageBusTest() {
        var message_01 = new SampleMessage("Message_01");
        var message_02 = new SampleMessage("Message_02");
        var message_03 = new SampleMessage("Message_03");
        var message_04 = new SampleMessage("Message_04");
        
        MessageBus.Subscribe(new SampleMessageHandler());
        
        MessageBus.Publish(message_01);
        MessageBus.Publish(message_02);
        MessageBus.Publish(message_03);
        MessageBus.Publish(message_04);
        
        MessageBus.Unsubscribe<SampleMessage>();
    }

    [Test]
    public async Task MessageBusAsyncTest() {
        var message_01 = new SampleMessage("Message_01");
        var message_02 = new SampleMessage("Message_02");
        var message_03 = new SampleMessage("Message_03");
        var message_04 = new SampleMessage("Message_04");
        
        MessageBus.Subscribe(new SampleMessageHandler());

        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));

        var handler_02 = new SampleMessageHandler_02();
        MessageBus.Subscribe(handler_02);
        
        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));
        
        MessageBus.Unsubscribe(handler_02);
        
        await Task.WhenAll(MessageBus.PublishAsync(message_01), MessageBus.PublishAsync(message_02), MessageBus.PublishAsync(message_03), MessageBus.PublishAsync(message_04));
        
        MessageBus.Unsubscribe<SampleMessage>();
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
}

public class SampleMessageHandler_02 : IAsyncMessageHandler<SampleMessage> {

    public void Handle(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
    }

    public Task HandleAsync(SampleMessage message) {
        message.ThrowIfNull();
        Logger.TraceLog(message.message);
        return Task.CompletedTask;
    }
}