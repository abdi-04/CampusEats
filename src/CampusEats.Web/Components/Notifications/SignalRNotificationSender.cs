using CampusEats.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CampusEats.Web.Components.Notifications;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationSender(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    // Send to a specific user's group
    public async Task SendToUserAsync(string userId, string method, object data)
    {
        await _hub.Clients
            .Group($"user-{userId}")
            .SendAsync(method, data);
    }

    // Send to any filtered group
    public async Task SendToGroupAsync(string groupName, string method, object data)
    {
        await _hub.Clients
            .Group(groupName)
            .SendAsync(method, data);
    }
}