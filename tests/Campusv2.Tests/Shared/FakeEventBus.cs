using CampusEatsv2.Infrastructure.Messaging;

public sealed class FakeEventBus : IEventBus
{
    public Task PublishAsync(
        string exchange,
        string routingKey,
        object message)
    {
        // Intentionally do nothing
        return Task.CompletedTask;
    }
}