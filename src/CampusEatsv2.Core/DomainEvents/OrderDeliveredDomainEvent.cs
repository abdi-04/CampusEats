// Catches OrderDelivered event and used for email service

using MediatR;

namespace CampusEatsv2.Core.DomainEvents;

public record OrderDeliveredDomainEvent(
    Guid OrderId,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal Tip
) : INotification;