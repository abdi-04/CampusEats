namespace CampusEatsv2.Core.Interfaces;
// Used for push-notifications
// Defines method that are used to send them
public interface INotificationSender
{
    Task SendToUserAsync(string userId, string method, object data);
    Task SendToGroupAsync(string groupName, string method, object data);
}