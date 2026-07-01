using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class GetAllCouriersQueryTests
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

    private async Task<Courier> CreateCourier(CourierStatus status = CourierStatus.Approved)
    {
        var hasher = new PasswordHasher<Courier>();
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = "courier@example.com",
            PasswordHash = hasher.HashPassword(new Courier { FullName = "Test Courier", Email = "courier@example.com", PasswordHash = "" }, "courierPassword123"),
            Status = status
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    [Test]
    public async Task GetAllCouriersQuery_ReturnsAllCouriers()
    {
        var courier1 = await CreateCourier();
        var courier2 = await CreateCourier();

        var query = new GetAllCouriers.GetAllCouriersQuery();
        var couriers = await _mediator.Send(query);

        Assert.That(couriers, Has.Count.EqualTo(2));
        Assert.That(couriers, Contains.Item(courier1));
        Assert.That(couriers, Contains.Item(courier2));
    }

    [Test]
    public async Task GetAllCouriersQuery_ReturnsEmptyList_WhenNoCouriers()
    {
        var query = new GetAllCouriers.GetAllCouriersQuery();
        var couriers = await _mediator.Send(query);

        Assert.That(couriers, Is.Empty);
    }

    [Test]
    public async Task GetAllCouriersQuery_GetsAllCouriers()
    {
        var approvedCourier = await CreateCourier(CourierStatus.Approved);
        var pendingCourier = await CreateCourier(CourierStatus.Pending);
        var declinedCourier = await CreateCourier(CourierStatus.Declined);

        var query = new GetAllCouriers.GetAllCouriersQuery();
        var couriers = await _mediator.Send(query);

        Assert.That(couriers, Has.Count.EqualTo(3));
        Assert.That(couriers, Contains.Item(approvedCourier));
        Assert.That(couriers, Contains.Item(pendingCourier));
        Assert.That(couriers, Contains.Item(declinedCourier));
    }

    [Test]
    public async Task GetAllCouriersQuery_PerformanceTest()
    {
        for (int i = 0; i < 1000; i++)
        {
            await CreateCourier();
        }

        var query = new GetAllCouriers.GetAllCouriersQuery();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var couriers = await _mediator.Send(query);
        watch.Stop();

        Assert.That(couriers, Has.Count.EqualTo(1000));
        Assert.That(watch.ElapsedMilliseconds, Is.LessThan(500));
    }
}