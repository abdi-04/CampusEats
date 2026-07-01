using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ApproveCourierRegistrationTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Required for MediatR + handlers
        services.AddLogging();

        // In-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // MediatR: command + domain event handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(ApproveCourierRegistration.ApproveCourierRegistrationHandler).Assembly);

            cfg.RegisterServicesFromAssembly(
                typeof(CourierApprovedDomainEventHandler).Assembly);
        });

        // Validation pipeline
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));

        services.AddSingleton<IEventBus, FakeEventBus>();

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
    public async Task ApproveCourier_WhenPending_UpdatesStatusToApproved()
    {
        var courier = await CreateCourier(CourierStatus.Pending);

        var command = new ApproveCourierRegistration.ApproveCourierRegistrationCommand
        {
            CourierId = courier.CourierId
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Status, Is.EqualTo(CourierStatus.Approved));
    }

    [Test]
    public void ApproveCourier_WhenCourierDoesNotExist_ThrowsException()
    {
        var command = new ApproveCourierRegistration.ApproveCourierRegistrationCommand
        {
            CourierId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task ApproveCourier_WhenNotPending_ThrowsException()
    {
        var courier = await CreateCourier(CourierStatus.Approved);

        var command = new ApproveCourierRegistration.ApproveCourierRegistrationCommand
        {
            CourierId = courier.CourierId
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));
    }
}