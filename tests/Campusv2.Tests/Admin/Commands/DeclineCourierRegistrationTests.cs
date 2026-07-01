using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class DeclineCourierRegistrationTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Logging (required by handlers)
        services.AddLogging();

        // In‑memory EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // MediatR: command + domain events
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(DeclineCourierRegistration.DeclineCourierRegistrationHandler).Assembly);

            cfg.RegisterServicesFromAssembly(
                typeof(CourierDeclinedDomainEventHandler).Assembly);
        });

        // Validation pipeline
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));

        // ✅ Fake infrastructure
        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddSingleton<INotificationSender, FakeNotificationSender>();
        services.AddSingleton<INotificationStore, FakeNotificationStore>();

        var provider = services.BuildServiceProvider();

        _mediator = provider.GetRequiredService<IMediator>();
        _context = provider.GetRequiredService<AppDbContext>();

        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // Helper
    private async Task<Courier> CreateCourier(CourierStatus status)
    {
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = "courier@test.com",
            PasswordHash = "hashed-password",
            Status = status
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    [Test]
    public async Task DeclineCourier_WhenPending_UpdatesStatusToDeclined()
    {
        var courier = await CreateCourier(CourierStatus.Pending);

        var command = new DeclineCourierRegistration.DeclineCourierRegistrationCommand
        {
            CourierId = courier.CourierId
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Status, Is.EqualTo(CourierStatus.Declined));
    }

    [Test]
    public void DeclineCourier_WhenCourierDoesNotExist_ThrowsException()
    {
        var command = new DeclineCourierRegistration.DeclineCourierRegistrationCommand
        {
            CourierId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _mediator.Send(command));
    }

    [Test]
    public async Task DeclineCourier_WhenNotPending_ThrowsException()
    {
        var courier = await CreateCourier(CourierStatus.Declined);

        var command = new DeclineCourierRegistration.DeclineCourierRegistrationCommand
        {
            CourierId = courier.CourierId
        };

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(command));
    }
}