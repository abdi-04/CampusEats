// Catches CourierDeclined event and used for email service

using MediatR;

namespace CampusEats.Core.DomainEvents;

public sealed record CourierDeclinedDomainEvent(
    Guid CourierId,
    string FullName,
    string Email
) : INotification;