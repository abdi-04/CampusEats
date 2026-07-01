using MediatR;
using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Messaging.Events;

namespace CampusEatsv2.Infrastructure;
// This fails handle certain events happening in the backend
// This is needed for when something occurs to actully act on it

public sealed class CourierApprovedDomainEventHandler
    : INotificationHandler<CourierApprovedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public CourierApprovedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(CourierApprovedDomainEvent notification, CancellationToken ct)
    {
        return _eventBus.PublishAsync(
            "campuseats.events",
            "courier.approved",
            new CourierApprovedIntegrationEvent(
                notification.CourierId,
                notification.FullName,
                notification.Email));
    }
}
