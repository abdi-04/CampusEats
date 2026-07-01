using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace CampusEatsv2.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ-based implementation of the event bus.
/// 
/// Responsible for publishing integration events to a RabbitMQ exchange
/// using topic routing. This event bus is intended for cross-service
/// communication and integration events only.
/// </summary>

public sealed class RabbitMqEventBus : IEventBus
{
    private readonly ConnectionFactory _factory;

    public RabbitMqEventBus(IConfiguration configuration)
    {
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"]!,
            UserName = configuration["RabbitMq:User"]!,
            Password = configuration["RabbitMq:Password"]!,
            DispatchConsumersAsync = true
        };
    }

    public Task PublishAsync(
        string exchange,
        string routingKey,
        object message)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true);

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message));

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body);

        return Task.CompletedTask;
    }
}