// Catches OrderDelivered event and used for email service

using MediatR;

namespace CampusEats.Core.DomainEvents;

public record OrderDeliveredDomainEvent(
    Guid OrderId,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal Tip
) : INotification;