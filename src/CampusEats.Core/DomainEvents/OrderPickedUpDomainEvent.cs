// Catches OrderPickedup event and used for email service

using MediatR;

namespace CampusEats.Core.DomainEvents;

public sealed record OrderPickedUpDomainEvent(
    Guid OrderId
) : INotification;
