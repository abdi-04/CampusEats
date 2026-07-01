using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CourierServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class GetCourierOrdersQueryTests
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

    private async Task<Order> CreateOrder()
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CourierId = Guid.NewGuid(),
            Status = OrderStatus.Submitted,            
            CreationTime = DateTime.UtcNow,
            PickupAddress = "123 Test St",
        };    
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    [Test]
    public async Task GetCourierOrdersQuery_ReturnsCourierOrders()
    {
        var courierId = Guid.NewGuid();
        var order1 = await CreateOrder();
        order1.CourierId = courierId;
        var order2 = await CreateOrder();
        order2.CourierId = courierId;
        await _context.SaveChangesAsync();

        var query = new GetCourierOrdersQuery.GetCourierOrdersQueryRequest { CourierId = courierId };
        var orders = await _mediator.Send(query);

        Assert.That(orders, Has.Count.EqualTo(2));
        Assert.That(orders[0].OrderId, Is.EqualTo(order1.OrderId));
        Assert.That(orders[1].OrderId, Is.EqualTo(order2.OrderId));
    }

    [Test]
    public async Task GetCourierOrdersQuery_ReturnsEmptyListWhenNoOrders()
    {
        var courierId = Guid.NewGuid();
        var query = new GetCourierOrdersQuery.GetCourierOrdersQueryRequest { CourierId = courierId };
        var orders = await _mediator.Send(query);

        Assert.That(orders, Is.Empty);
    }

    [Test]
    public async Task GetCourierOrdersQuery_ReturnsCorrectOrderDetails()
    {
        var courierId = Guid.NewGuid();
        var order = await CreateOrder();
        order.CourierId = courierId;
        order.Status = OrderStatus.Delivered;
        await _context.SaveChangesAsync();

        var query = new GetCourierOrdersQuery.GetCourierOrdersQueryRequest { CourierId = courierId };
        var orders = await _mediator.Send(query);

        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(orders[0].OrderId, Is.EqualTo(order.OrderId));
        Assert.That(orders[0].Status, Is.EqualTo(OrderStatus.Delivered));
    }    

    [Test]
    public async Task GetCourierOrdersQuery_ReturnsOnlyCourierOrders()
    {
        var courierId1 = Guid.NewGuid();
        var courierId2 = Guid.NewGuid();

        var order1 = await CreateOrder();
        order1.CourierId = courierId1;

        var order2 = await CreateOrder();
        order2.CourierId = courierId2;

        await _context.SaveChangesAsync();

        var query = new GetCourierOrdersQuery.GetCourierOrdersQueryRequest { CourierId = courierId1 };
        var orders = await _mediator.Send(query);

        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(orders[0].OrderId, Is.EqualTo(order1.OrderId));
    }

    [Test]
    public async Task GetCourierOrdersQuery_PerformanceTest()
    {
        var courierId = Guid.NewGuid();
        for (int i = 0; i < 100; i++)
        {
            var order = await CreateOrder();
            order.CourierId = courierId;
        }
        await _context.SaveChangesAsync();
        var query = new GetCourierOrdersQuery.GetCourierOrdersQueryRequest { CourierId = courierId };
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var orders = await _mediator.Send(query);
        watch.Stop();
        Assert.That(orders, Has.Count.EqualTo(100));
        Assert.That(watch.ElapsedMilliseconds, Is.LessThan(500), "Query took too long to execute");
    }
}