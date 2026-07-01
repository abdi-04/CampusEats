using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ManageCourierRegistrationsTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var serviceProvider = Program.ConfigureServices(useInMemory: true);
        _mediator = serviceProvider.GetRequiredService<IMediator>();
        _context = serviceProvider.GetRequiredService<AppDbContext>();
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateCourier(CourierStatus status)
    {
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = "test@test.com",
            PasswordHash = "hashed",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task ManageCouriers_ReturnsOnlyPendingCouriers()
    {
        // Arrange
        await CreateCourier(CourierStatus.Pending);
        await CreateCourier(CourierStatus.Pending);
        await CreateCourier(CourierStatus.Approved);
        await CreateCourier(CourierStatus.Declined);

        var query = new ManageCourierRegistrations.ManageCourierRegistrationsQuery();

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(c => c.Status == CourierStatus.Pending), Is.True);
    }

    [Test]
    public async Task ManageCouriers_NoPending_ReturnsEmptyList()
    {
        // Arrange
        await CreateCourier(CourierStatus.Approved);
        await CreateCourier(CourierStatus.Declined);

        var query = new ManageCourierRegistrations.ManageCourierRegistrationsQuery();

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ManageCouriers_EmptyDatabase_ReturnsEmptyList()
    {
        var query = new ManageCourierRegistrations.ManageCourierRegistrationsQuery();

        var result = await _mediator.Send(query);

        Assert.That(result, Is.Empty);
    }
}