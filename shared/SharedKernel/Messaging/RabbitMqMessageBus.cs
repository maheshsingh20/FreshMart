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

    private void EnsureConnected()
    {
        if (_connection?.IsOpen == true) return;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection("GroceryPlatform");
        _channel = _connection.CreateModel();
        logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
    }

    public Task PublishAsync<T>(T message, string topic, CancellationToken ct = default) where T : class
    {
        EnsureConnected();
        _channel!.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Type = typeof(T).Name;
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(topic, "", props, body);
        logger.LogDebug("Published {EventType} to {Topic}", typeof(T).Name, topic);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class
    {
        EnsureConnected();
        _channel!.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
        var queue = _channel.QueueDeclare(exclusive: false, durable: true, autoDelete: false);
        _channel.QueueBind(queue.QueueName, topic, "");

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

        _channel.BasicConsume(queue.QueueName, false, consumer);
        return Task.CompletedTask;
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
    };

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        var topic = TopicMap.TryGetValue(typeof(T), out var t) ? t : typeof(T).Name.ToLower();
        return bus.PublishAsync(integrationEvent, topic, ct);
    }
}
