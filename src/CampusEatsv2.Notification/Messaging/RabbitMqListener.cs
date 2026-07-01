using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CampusEatsv2.Notification.Consumers;
using CampusEatsv2.Infrastructure.Messaging.Events;

/// <summary>
/// Background service that listens for integration events from RabbitMQ
/// and dispatches them to the appropriate notification consumers.
/// 
/// This listener subscribes to a topic exchange and reacts to specific
/// routing keys representing events published by other services.
/// </summary>

public class RabbitMqListener : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;

    public RabbitMqListener(
        IConfiguration config,
        IServiceProvider services)
    {
        _config = config;
        _services = services;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"]!,
            UserName = _config["RabbitMq:User"]!,
            Password = _config["RabbitMq:Password"]!
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare("campuseats.events", ExchangeType.Topic, true);

        var queue = channel.QueueDeclare().QueueName;

        // Bind ALL events
        channel.QueueBind(queue, "campuseats.events", "customer.registered");
        channel.QueueBind(queue, "campuseats.events", "order.delivered");
        channel.QueueBind(queue, "campuseats.events", "order.pickedup");
        channel.QueueBind(queue, "campuseats.events", "courier.approved");
        channel.QueueBind(queue, "campuseats.events", "courier.declined");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            using var scope = _services.CreateScope();
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            switch (ea.RoutingKey)
            {
                case "customer.registered":
                {
                    var evt = JsonSerializer.Deserialize<CustomerRegisteredIntegrationEvent>(json)!;
                    var handler = scope.ServiceProvider.GetRequiredService<CustomerRegisteredConsumer>();
                    await handler.Handle(evt);
                    break;
                }

                case "order.delivered":
                {
                    var evt = JsonSerializer.Deserialize<OrderDeliveredIntegrationEvent>(json)!;
                    var handler = scope.ServiceProvider.GetRequiredService<OrderDeliveredConsumer>();
                    await handler.Handle(evt);
                    break;
                }

                case "courier.approved":
                {
                    var evt = JsonSerializer.Deserialize<CourierApprovedIntegrationEvent>(json)!;
                    var handler = scope.ServiceProvider.GetRequiredService<CourierApprovedConsumer>();
                    await handler.Handle(evt);
                    break;
                }

                case "courier.declined":
                {
                    var evt = JsonSerializer.Deserialize<CourierDeclinedIntegrationEvent>(json)!;
                    var handler = scope.ServiceProvider.GetRequiredService<CourierDeclinedConsumer>();
                    await handler.Handle(evt);
                    break;   
                }

                case "order.pickedup":
                {
                    var evt = JsonSerializer.Deserialize<OrderPickedUpIntegrationEvent>(json)!;
                    var handler = scope.ServiceProvider.GetRequiredService<OrderPickedUpConsumer>();
                    await handler.Handle(evt);
                    break;
                }

            }
        };

        channel.BasicConsume(queue, autoAck: true, consumer);

        return Task.CompletedTask;
    }
}