using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CampusEatsv2.IntegrationTests.Helper;
// Helper used by integration tests to publish integration events
// directly to RabbitMQ. This publisher bypasses application infrastructure and is intended
// solely for test scenarios where events need to be injected into

public static class IntegrationTestPublisher
{
    private const string Exchange = "campuseats.events";

    public static void PublishIntegrationEvent(
        string routingKey,
        object payload)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(payload));

        channel.BasicPublish(
            exchange: Exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body);
    }
}