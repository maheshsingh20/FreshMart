using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace SharedKernel.Messaging;

public class RabbitMqMessageBus(IConfiguration config, ILogger<RabbitMqMessageBus> logger)
    : IMessageBus, IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;

    private ConnectionFactory BuildFactory() => new()
    {
        HostName = config["RabbitMQ:Host"] ?? "localhost",
        Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
        UserName = config["RabbitMQ:Username"] ?? "guest",
        Password = config["RabbitMQ:Password"] ?? "guest",
        VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
        DispatchConsumersAsync = true,
        RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
    };

    /// <summary>
    /// Tries to connect with up to <paramref name="maxAttempts"/> retries.
    /// Used at subscribe time so the consumer survives a slow RabbitMQ startup.
    /// </summary>
    private async Task<bool> EnsureConnectedAsync(int maxAttempts = 10, CancellationToken ct = default)
    {
        if (_connection?.IsOpen == true) return true;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var factory = BuildFactory();
                _connection = factory.CreateConnection("GroceryPlatform");
                _channel = _connection.CreateModel();
                logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("RabbitMQ connect attempt {Attempt}/{Max} failed: {Message}", attempt, maxAttempts, ex.Message);
                if (attempt < maxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt * 2, 30)), ct);
            }
        }
        return false;
    }

    /// <summary>Sync connect used for publish (fire-and-forget path).</summary>
    private bool TryEnsureConnected()
    {
        if (_connection?.IsOpen == true) return true;
        try
        {
            var factory = BuildFactory();
            _connection = factory.CreateConnection("GroceryPlatform");
            _channel = _connection.CreateModel();
            logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("RabbitMQ unavailable — publish skipped. {Message}", ex.Message);
            return false;
        }
    }

    public Task PublishAsync<T>(T message, string topic, CancellationToken ct = default) where T : class
    {
        if (!TryEnsureConnected())
        {
            logger.LogWarning("Skipping publish of {EventType} to {Topic} — RabbitMQ not connected", typeof(T).Name, topic);
            return Task.CompletedTask;
        }
        try
        {
            _channel!.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.Type = typeof(T).Name;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _channel.BasicPublish(topic, "", props, body);
            logger.LogDebug("Published {EventType} to {Topic}", typeof(T).Name, topic);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to publish {EventType}: {Message}", typeof(T).Name, ex.Message);
        }
        return Task.CompletedTask;
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class
    {
        if (!await EnsureConnectedAsync(ct: ct))
        {
            logger.LogError("Giving up subscribe to {Topic} — RabbitMQ unreachable after retries", topic);
            return;
        }
        try
        {
            _channel!.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
            var queueName = $"{topic}.notification-service";
            _channel.QueueDeclare(queueName, exclusive: false, durable: true, autoDelete: false);
            _channel.QueueBind(queueName, topic, "");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonConvert.DeserializeObject<T>(json)!;
                    await handler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from {Topic}", topic);
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                }
            };
            _channel.BasicConsume(queueName, false, consumer);
            logger.LogInformation("Subscribed to {Topic} via queue {Queue}", topic, queueName);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to subscribe to {Topic}: {Message}", topic, ex.Message);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}

public class RabbitMqEventPublisher(IMessageBus bus) : IEventPublisher
{
    private static readonly Dictionary<Type, string> TopicMap = new()
    {
        [typeof(Events.OrderCreatedEvent)] = "order.created",
        [typeof(Events.PaymentCompletedEvent)] = "payment.completed",
        [typeof(Events.PaymentFailedEvent)] = "payment.failed",
        [typeof(Events.InventoryUpdatedEvent)] = "inventory.updated",
        [typeof(Events.DeliveryAssignedEvent)] = "delivery.assigned",
        [typeof(Events.OrderCancelledEvent)] = "order.cancelled",
        [typeof(Events.LowStockAlertEvent)] = "inventory.low-stock",
        [typeof(Events.OrderStatusChangedEvent)] = "order.status-changed",
    };

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        var topic = TopicMap.TryGetValue(typeof(T), out var t) ? t : typeof(T).Name.ToLower();
        return bus.PublishAsync(integrationEvent, topic, ct);
    }
}
