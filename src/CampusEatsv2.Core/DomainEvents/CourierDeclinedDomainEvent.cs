// Catches CourierDeclined event and used for email service

using MediatR;

namespace CampusEatsv2.Core.DomainEvents;

public sealed record CourierDeclinedDomainEvent(
    Guid CourierId,
    string FullName,
    string Email
) : INotification;