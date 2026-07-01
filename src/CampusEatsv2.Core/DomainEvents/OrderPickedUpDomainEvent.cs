// Catches OrderPickedup event and used for email service

using MediatR;

namespace CampusEatsv2.Core.DomainEvents;

public sealed record OrderPickedUpDomainEvent(
    Guid OrderId
) : INotification;
