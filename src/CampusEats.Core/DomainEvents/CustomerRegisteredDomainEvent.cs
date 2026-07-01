// Catches CustomerRegistered event and used for email service

using MediatR;

namespace CampusEats.Core.DomainEvents;

public sealed record CustomerRegisteredDomainEvent(
    Guid CustomerId,
    string Email,
    string FullName
) : INotification;