namespace SharedKernel.Messaging;

/// <summary>
/// Abstraction over the underlying message broker (RabbitMQ).
/// Decouples services from the broker implementation so it can be swapped or mocked in tests.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the specified topic exchange.
    /// Fire-and-forget — if the broker is unavailable the message is silently dropped and a warning is logged.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="topic">The RabbitMQ fanout exchange name (e.g. "order.created").</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishAsync<T>(T message, string topic, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Subscribes to a topic exchange and invokes <paramref name="handler"/> for each received message.
    /// Retries connection up to 10 times with exponential back-off to survive slow broker startup.
    /// </summary>
    /// <typeparam name="T">The expected message payload type.</typeparam>
    /// <param name="topic">The RabbitMQ fanout exchange name to subscribe to.</param>
    /// <param name="handler">Async callback invoked for each deserialized message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class;
}

/// <summary>
/// Higher-level publisher that maps integration event types to their canonical topic names automatically.
/// Prefer this over <see cref="IMessageBus"/> in application handlers to avoid hard-coding topic strings.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an integration event to its pre-configured topic.
    /// The topic is resolved from an internal type-to-topic map; unknown types fall back to the lowercased type name.
    /// </summary>
    /// <typeparam name="T">The integration event type.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class;
}
