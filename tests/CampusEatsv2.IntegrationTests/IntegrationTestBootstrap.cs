using CampusEatsv2.IntegrationTests.Helper;
using NUnit.Framework;

namespace CampusEatsv2.IntegrationTests;

// NUnit bootstrap for integration testing
// Executed once per test is required by integration tests to actully push a notification

[SetUpFixture]
public sealed class IntegrationTestBootstrap
{
    private TestNotificationHost? _host;

    [OneTimeSetUp]
    public async Task StartNotification()
    {
        _host = await TestNotificationHost.StartAsync();

        // Let RabbitMQ subscriptions settle
        await Task.Delay(1000);
    }

    [OneTimeTearDown]
    public async Task StopNotification()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }
}
