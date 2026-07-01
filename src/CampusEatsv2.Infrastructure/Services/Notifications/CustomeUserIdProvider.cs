using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CampusEatsv2.Web.Components.Notifications;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;
    }
}