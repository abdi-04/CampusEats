using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CustomerServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class GetCustomerOrdersQueryTests
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
    public async Task GetOrdersQuery_ReturnsCustomerOrders()
    {
        var customerId = Guid.NewGuid();
        var order1 = await CreateOrder();
        order1.CustomerId = customerId;
        var order2 = await CreateOrder();
        order2.CustomerId = customerId;
        await _context.SaveChangesAsync();

        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId };
        var orders = await _mediator.Send(query);

        Assert.AreEqual(2, orders.Count);
        Assert.IsTrue(orders.All(o => o.CustomerId == customerId));
    }

    [Test]
    public async Task GetOrdersQuery_ReturnsEmptyListForCustomerWithNoOrders()
    {
        var customerId = Guid.NewGuid();

        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId };
        var orders = await _mediator.Send(query);

        Assert.IsNotNull(orders);
        Assert.AreEqual(0, orders.Count);
    }

    [Test]
    public async Task GetOrdersQuery_ReturnsCorrectOrderDetails()
    {
        var customerId = Guid.NewGuid();
        var order = await CreateOrder();
        order.CustomerId = customerId;
        await _context.SaveChangesAsync();

        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId };
        var orders = await _mediator.Send(query);

        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(order.OrderId, orders[0].OrderId);
        Assert.AreEqual(order.Status, orders[0].Status);
    }

    [Test]
    public async Task GetOrdersQuery_ReturnsOnlyOrdersForSpecifiedCustomer()
    {
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();

        var order1 = await CreateOrder();
        order1.CustomerId = customerId1;

        var order2 = await CreateOrder();
        order2.CustomerId = customerId2;

        await _context.SaveChangesAsync();

        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId1 };
        var orders = await _mediator.Send(query);

        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual(order1.OrderId, orders[0].OrderId);
    }

    [Test]
    public async Task GetOrdersQuery_ReturnsEmptyListWhenNoOrdersForCustomer()
    {
        var customerId = Guid.NewGuid();

        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId };
        var orders = await _mediator.Send(query);

        Assert.IsNotNull(orders);
        Assert.AreEqual(0, orders.Count);
    }

    [Test]
    public async Task GetOrdersQuery_PerformanceTest()
    {
        var customerId = Guid.NewGuid();
        for (int i = 0; i < 100; i++)
        {
            var order = await CreateOrder();
            order.CustomerId = customerId;
        }
        await _context.SaveChangesAsync();
        var query = new GetCustomerOrdersQuery.GetCustomerOrdersQueryRequest { CustomerId = customerId };
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var orders = await _mediator.Send(query);
        watch.Stop();
        Assert.AreEqual(100, orders.Count);
        Assert.Less(watch.ElapsedMilliseconds, 500); // Ensure it returns within 500ms for perfomance testing
    }
}