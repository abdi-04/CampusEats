using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Core.Models;
using MediatR;

namespace CampusEatsv2.Infrastructure.Services.Notifications;

public class OrderStatusChangedHandler : INotificationHandler<OrderStatusChangedNotification>
{
    private readonly INotificationSender _sender;
    private readonly INotificationStore _store;

    public OrderStatusChangedHandler(
        INotificationSender sender,
        INotificationStore store)
    {
        _sender = sender;
        _store = store;
    }

    public async Task Handle(
        OrderStatusChangedNotification notification,
        CancellationToken ct)
    {
        var payload = new
        {
            notification.OrderId,
            Status = notification.NewStatus.ToString(),
            Timestamp = DateTime.UtcNow
        };

        _store.Add(new AppNotification
        {
            RecipientId = notification.CustomerId,
            OrderId = notification.OrderId,

            RecipientLabel = $"Order #{notification.OrderId.ToString()[..8]}",

            Status = notification.NewStatus,
            Timestamp = DateTime.UtcNow
        });

        await _sender.SendToUserAsync(
            notification.CustomerId.ToString(),
            "OrderStatusChanged",
            payload);
    }
}