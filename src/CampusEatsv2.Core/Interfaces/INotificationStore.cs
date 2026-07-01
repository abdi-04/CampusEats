using CampusEatsv2.Core.Models;

namespace CampusEatsv2.Core.Interfaces;
// Used for push-notifications
// Defines method that are used to get events and create a small container to store them
public interface INotificationStore
{
    void Add(AppNotification notification);
    List<AppNotification> GetForUser(Guid userId);
    int GetUnreadCount(Guid userId);
    void MarkAllRead(Guid userId);
}