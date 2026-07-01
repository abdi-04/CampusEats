using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Messaging.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.DomainEvents.Handlers;
// This fails handle certain events happening in the backend
// This is needed for when something occurs to actully act on it

public sealed class OrderDeliveredDomainEventHandler
    : INotificationHandler<OrderDeliveredDomainEvent>
{
    private readonly AppDbContext _context;
    private readonly IEventBus _eventBus;

    public OrderDeliveredDomainEventHandler(
        AppDbContext context,
        IEventBus eventBus)
    {
        _context = context;
        _eventBus = eventBus;
    }

    public async Task Handle(
        OrderDeliveredDomainEvent notification,
        CancellationToken ct)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == notification.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found");

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId, ct)
            ?? throw new InvalidOperationException("Customer not found");

        await _eventBus.PublishAsync(
            "campuseats.events",
            "order.delivered",
            new OrderDeliveredIntegrationEvent(
                notification.OrderId,
                customer.Email,      
                customer.FullName,   
                notification.TotalPrice,
                notification.DeliveryFee,
                notification.Tip));
    }
}