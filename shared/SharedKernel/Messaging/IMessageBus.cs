namespace SharedKernel.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string topic, CancellationToken ct = default) where T : class;
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class;
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class;
}
