namespace CampusEatsv2.Infrastructure.Messaging;
// Make a simple event bus for transfer
// Used for cross communication between services

public interface IEventBus
{
    Task PublishAsync(
        string exchange,
        string routingKey,
        object message);
}