using Microsoft.AspNetCore.SignalR;

namespace CampusEatsv2.Web.Components.Notifications;

public class NotificationHub : Hub
{
    // Client calls this once connected passing CustomerId or CourierId
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}