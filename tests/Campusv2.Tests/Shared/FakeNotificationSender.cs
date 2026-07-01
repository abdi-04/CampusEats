using CampusEatsv2.Core.Interfaces;

internal sealed class FakeNotificationSender : INotificationSender
{
    public Task SendToUserAsync(string userId, string method, object data)
    {
        // no-op for unit tests
        return Task.CompletedTask;
    }

    public Task SendToGroupAsync(string groupName, string method, object data)
    {
        // no-op for unit tests
        return Task.CompletedTask;
    }
}