using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Core.Models;

namespace CampusEatsv2.Web.Components.Notifications;

public class NotificationStore : INotificationStore
{
    private readonly List<AppNotification> _notifications = new();

    public void Add(AppNotification notification)
        => _notifications.Add(notification);

    public List<AppNotification> GetForUser(Guid userId)
        => _notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.Timestamp)
            .ToList();

    public int GetUnreadCount(Guid userId)
        => _notifications.Count(n => n.RecipientId == userId && !n.IsRead);

    public void MarkAllRead(Guid userId)
    {
        foreach (var n in _notifications.Where(n => n.RecipientId == userId))
            n.IsRead = true;
    }
}