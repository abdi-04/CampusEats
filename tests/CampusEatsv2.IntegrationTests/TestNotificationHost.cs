using CampusEatsv2.Notification.Consumers;
using CampusEatsv2.Notification.Messaging;
using CampusEatsv2.Notification.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CampusEatsv2.IntegrationTests.Helper;

// Test-only host used to run the Notification service during integration tests.
// This helper bootstraps a minimal application host that mirrors the
// production notification setup while allowing configuration overrides
// and controlled startup/shutdown for 

public sealed class TestNotificationHost : IAsyncDisposable
{
    private readonly IHost _host;

    private TestNotificationHost(IHost host)
    {
        _host = host;
    }

    public static async Task<TestNotificationHost> StartAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        // Override configuration for integration tests
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:User"] = "guest",
            ["RabbitMq:Password"] = "guest"
        });

        // Same registrations as Program.cs
        builder.Services.AddSingleton<SmtpEmailSender>();

        builder.Services.AddScoped<CustomerRegisteredConsumer>();
        builder.Services.AddScoped<OrderDeliveredConsumer>();
        builder.Services.AddScoped<OrderPickedUpConsumer>();
        builder.Services.AddScoped<CourierApprovedConsumer>();
        builder.Services.AddScoped<CourierDeclinedConsumer>();

        builder.Services.AddHostedService<RabbitMqWorker>();

        builder.Environment.EnvironmentName = "IntegrationTests";

        var host = builder.Build();
        await host.StartAsync();

        return new TestNotificationHost(host);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
