using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.Notifications;

public class PostOrderNotificationHandler : INotificationHandler<PostOrderNotification>
{
    private readonly INotificationSender _sender;
    private readonly AppDbContext _context;
    private readonly INotificationStore _store;


    public PostOrderNotificationHandler(
        AppDbContext context,
        INotificationSender sender,
        INotificationStore store)
    {
        _context = context;
        _sender = sender;
        _store = store;
    }

    public async Task Handle(PostOrderNotification notification, CancellationToken ct)
    {
        var couriers = await _context.Couriers
            .Where(c => c.Status == CourierStatus.Approved)
            .ToListAsync(ct);

        var payload = new
        {
            notification.OrderId,
            notification.CustomerId,
            Status = OrderStatus.Submitted.ToString(),
            Timestamp = DateTime.UtcNow
        };

        foreach (var courier in couriers)
        {
            // Persist to in-memory store
            _store.Add(new AppNotification
            {
                RecipientId = courier.CourierId,
                OrderId = notification.OrderId,
                RecipientLabel = $"Customer {notification.CustomerId.ToString()[..8]}",
                Status = OrderStatus.Submitted,
                Timestamp = DateTime.UtcNow
            });

            // Push via SignalR
            await _sender.SendToUserAsync(
                courier.CourierId.ToString(),
                "NewOrderAvailable",
                payload);
        }
    }
}