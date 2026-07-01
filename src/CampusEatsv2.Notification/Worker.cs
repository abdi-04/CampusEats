using CampusEatsv2.Notification.Consumers;
using CampusEatsv2.Infrastructure.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CampusEatsv2.Notification.Messaging;

/// <summary>
/// Background worker responsible for consuming integration events from RabbitMQ
/// and sending them to the appropriate consumers.
/// 
/// This worker connects to a topic exchange, subscribes to known routing keys,
/// and processes incoming messages.
/// </summary>

public sealed class RabbitMqWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<RabbitMqWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;
    private string? _queueName;

    public RabbitMqWorker(
        IConfiguration config,
        IServiceProvider services,
        ILogger<RabbitMqWorker> logger)
    {
        _config = config;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TryConnect();
        ConsumeMessages();

        _logger.LogInformation(" Connected to RabbitMQ and listening for messages.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void TryConnect()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"] ?? "rabbitmq",
            UserName = _config["RabbitMq:User"] ?? "guest",
            Password = _config["RabbitMq:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: "campuseats.events",
            type: ExchangeType.Topic,
            durable: true);

        _queueName = _channel.QueueDeclare(
            durable: false,
            exclusive: false,
            autoDelete: true).QueueName;

        // Bind ALL event types
        _channel.QueueBind(_queueName, "campuseats.events", "customer.registered");
        _channel.QueueBind(_queueName, "campuseats.events", "courier.approved");
        _channel.QueueBind(_queueName, "campuseats.events", "order.pickedup");
        _channel.QueueBind(_queueName, "campuseats.events", "order.delivered");
        _channel.QueueBind(_queueName, "campuseats.events", "courier.declined");
    }

    // Here we consume messages sent from the website
    private void ConsumeMessages()
    {
        if (_channel is null || _queueName is null)
            throw new InvalidOperationException("RabbitMQ not initialized");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            using var scope = _services.CreateScope();
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogInformation(" Received event with routing key: {RoutingKey}", ea.RoutingKey);

            try
            {   // Use a switch for switching between different events that can occur
                switch (ea.RoutingKey)
                {
                    case "customer.registered":
                    {
                        var evt = JsonSerializer.Deserialize<CustomerRegisteredIntegrationEvent>(json)!;
                        await scope.ServiceProvider
                            .GetRequiredService<CustomerRegisteredConsumer>()
                            .Handle(evt);
                        break;
                    }

                    case "courier.approved":
                    {
                        var evt = JsonSerializer.Deserialize<CourierApprovedIntegrationEvent>(json)!;
                        await scope.ServiceProvider
                            .GetRequiredService<CourierApprovedConsumer>()
                            .Handle(evt);
                        break;
                    }

                    case "courier.declined":
                    {
                        var evt = JsonSerializer.Deserialize<CourierDeclinedIntegrationEvent>(json)!;
                        await scope.ServiceProvider
                            .GetRequiredService<CourierDeclinedConsumer>()
                            .Handle(evt);
                        break;
                    }

                    case "order.pickedup":
                    {
                        var evt = JsonSerializer.Deserialize<OrderPickedUpIntegrationEvent>(json)!;
                        await scope.ServiceProvider
                            .GetRequiredService<OrderPickedUpConsumer>()
                            .Handle(evt);
                        break;
                    }

                    case "order.delivered":
                    {
                        var evt = JsonSerializer.Deserialize<OrderDeliveredIntegrationEvent>(json)!;
                        await scope.ServiceProvider
                            .GetRequiredService<OrderDeliveredConsumer>()
                            .Handle(evt);
                        break;
                    }

                    default:
                        _logger.LogWarning(
                            " Unhandled routing key: {RoutingKey}",
                            ea.RoutingKey);
                        break;
                }
            }
            // Catch if anythings happens in trying this!
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    " Failed to process message with routing key {RoutingKey}\nPayload:\n{Payload}",
                    ea.RoutingKey,
                    json);
            }
        };
        // Start consuming with auth acks
        _channel.BasicConsume(
            queue: _queueName,
            autoAck: true,   
            consumer: consumer);
    }
    // Close channel when worker is disposed! 
    public override void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        base.Dispose();
    }
}