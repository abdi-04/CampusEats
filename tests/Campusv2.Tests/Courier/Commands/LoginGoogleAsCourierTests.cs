using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CourierServices.LoginGoogleAsCourier;

namespace CampusEatsv2.Tests;

[TestFixture]
public class LoginGoogleAsCourierTests
{
    // null!:
    // 1) Suppresses nullable warnings.
    // 2) Fields are assigned in [SetUp].
    private IMediator _mediator = null!; // _mediator: used to send the login command under test.
    private AppDbContext _context = null!; // _context: direct access to the in‑memory database for setup/cleanup.

    // Helper method to create a courier in the database with specified email and status.
    // <summary>
    //Why this exists:
    // Avoids repeating seed logic in every test.Allows customizing:
    // Email
    // Courier status
    private async Task<Courier> CreateCourierAsync(
        string email = "courier@test.com",
        CourierStatus status = CourierStatus.Approved)
    {
        // Creates a real Courier entity.
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = email,
            PasswordHash = "",
            Status = status
        };

        // Adds the courier to the InMemory DB, Saves changes, and returns the created courier.
        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    // setup method runs before each test. It:
    // 1. Configures services to use an in-memory database.
    // 2. Retrieves the IMediator and AppDbContext instances for use in tests.
    [SetUp]
    public void Setup()
    {
        var serviceProvider = Program.ConfigureServices(useInMemory: true);

        _mediator = serviceProvider.GetRequiredService<IMediator>();
        _context = serviceProvider.GetRequiredService<AppDbContext>();

        _context.Database.EnsureCreated();
    }

    // tear down method runs after each test. It:
    // 1. Deletes the in-memory database to ensure a clean state for the next test
    // 2. Disposes the database context to free resources.

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task LoginGoogleAsCourier_ApprovedCourier_ReturnsCourier()
    {
        var seeded = await CreateCourierAsync(
            email: "courier@test.com",
            status: CourierStatus.Approved);

        var command = new Command { Email = "courier@test.com" };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CourierId, Is.EqualTo(seeded.CourierId));
        Assert.That(result.Email, Is.EqualTo(seeded.Email));
        Assert.That(result.FullName, Is.EqualTo(seeded.FullName));
    }

    [Test]
    public async Task LoginGoogleAsCourier_EmailNotFound_ThrowsInvalidOperationException()
    {
        await CreateCourierAsync(email: "courier@test.com");

        var command = new Command { Email = "nonexistent@test.com" };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("No courier application found"));
    }

    [Test]
    public async Task LoginGoogleAsCourier_PendingCourier_ThrowsUnauthorizedAccessException()
    {
        await CreateCourierAsync(
            email: "courier@test.com",
            status: CourierStatus.Pending);

        var command = new Command { Email = "courier@test.com" };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("still pending admin approval"));
    }

    [Test]
    public async Task LoginGoogleAsCourier_DeclinedCourier_ThrowsUnauthorizedAccessException()
    {
        await CreateCourierAsync(
            email: "courier@test.com",
            status: CourierStatus.Declined);

        var command = new Command { Email = "courier@test.com" };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("application was declined"));
    }
}