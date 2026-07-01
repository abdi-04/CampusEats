using MediatR;
using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Messaging.Events;

namespace CampusEatsv2.Infrastructure;
// This fails handle certain events happening in the backend
// This is needed for when something occurs to actully act on it

public sealed class CustomerRegisteredDomainEventHandler
    : INotificationHandler<CustomerRegisteredDomainEvent>
{
    private readonly IEventBus _eventBus;

    public CustomerRegisteredDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(
        CustomerRegisteredDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            exchange: "campuseats.events",
            routingKey: "customer.registered",
            message: new CustomerRegisteredIntegrationEvent
            (
                notification.CustomerId,
                notification.Email,
                notification.FullName
            ));
    }
}
