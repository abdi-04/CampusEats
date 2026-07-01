using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Core.Models;

internal sealed class FakeNotificationStore : INotificationStore
{
    private readonly List<AppNotification> _notifications = new();

    public void Add(AppNotification notification)
    {
        _notifications.Add(notification);
    }

    public List<AppNotification> GetForUser(Guid recipientId)
    {
        return _notifications
            .Where(n => n.RecipientId == recipientId)
            .ToList();
    }

    public int GetUnreadCount(Guid recipientId)
    {
        return _notifications.Count(n =>
            n.RecipientId == recipientId &&
            !n.IsRead);
    }

    public void MarkAllRead(Guid recipientId)
    {
        foreach (var notification in _notifications.Where(n => n.RecipientId == recipientId))
        {
            notification.IsRead = true;
        }
    }
}
