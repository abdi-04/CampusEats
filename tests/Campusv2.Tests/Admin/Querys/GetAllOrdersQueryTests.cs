using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class GetAllOrdersQueryTests
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
    public async Task GetAllOrdersQuery_ReturnsAllOrders()
    {
        var order1 = await CreateOrder();
        var order2 = await CreateOrder();

        var query = new GetAllOrdersQuery.GetAllOrdersQueryRequest();
        var orders = await _mediator.Send(query);

        Assert.That(orders, Has.Count.EqualTo(2));
        Assert.That(orders[0].OrderId, Is.EqualTo(order1.OrderId));
        Assert.That(orders[1].OrderId, Is.EqualTo(order2.OrderId));
        Assert.That(orders[0].Status, Is.EqualTo(OrderStatus.Submitted));
    }

    [Test]
    public async Task GetAllOrdersQuery_ReturnsEmptyListWhenNoOrders()
    {
        var query = new GetAllOrdersQuery.GetAllOrdersQueryRequest();
        var orders = await _mediator.Send(query);

        Assert.That(orders, Is.Empty);
    }

    [Test]
    public async Task GetAllOrdersQuery_ReturnsCorrectOrderDetails()
    {
        var order = await CreateOrder();

        var query = new GetAllOrdersQuery.GetAllOrdersQueryRequest();
        var orders = await _mediator.Send(query);

        Assert.That(orders, Has.Count.EqualTo(1));
        var orderDto = orders[0];
        Assert.That(orderDto.OrderId, Is.EqualTo(order.OrderId));
        Assert.That(orderDto.CustomerId, Is.EqualTo(order.CustomerId));
        Assert.That(orderDto.CourierId, Is.EqualTo(order.CourierId));
        Assert.That(orderDto.Status, Is.EqualTo(OrderStatus.Submitted));
        Assert.That(orderDto.CreationTime, Is.EqualTo(order.CreationTime));
    }

    [Test]
    public async Task GetAllOrdersQuery_PerformanceTest()
    {
        for (int i = 0; i < 1000; i++)
        {
            await CreateOrder();
        }

        var query = new GetAllOrdersQuery.GetAllOrdersQueryRequest();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var orders = await _mediator.Send(query);
        watch.Stop();

        Assert.That(orders, Has.Count.EqualTo(1000));
        Assert.That(watch.ElapsedMilliseconds, Is.LessThan(500), "Query took too long to execute");
    }
}