// Catches CourierApproved event and used for email service

using MediatR;

namespace CampusEats.Core.DomainEvents;

public sealed record CourierApprovedDomainEvent(
    Guid CourierId,
    string FullName,
    string Email
) : INotification;