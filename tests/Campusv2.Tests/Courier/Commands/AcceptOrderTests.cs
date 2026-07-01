using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CourierServices;
using CampusEatsv2.Web;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class AcceptOrderTests
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

    //  Helpers 

    // Creates and saves a new approved courier, returns the courier
    private async Task<Courier> CreateApprovedCourier()
    {
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = "courier@example.com",
            PasswordHash = "hashedpassword",
            Status = CourierStatus.Approved
        };
        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    // Creates and saves a new order with Submitted status, returns the order
    private async Task<Order> CreateSubmittedOrder()
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Submitted,
            PickupAddress = "123 Test St",
            CourierId = Guid.Empty, // no courier assigned yet
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    // Tests

    [Test]
    // Valid courier and order should assign courier and set status to Accepted
    public async Task AcceptOrder_ValidData_ReturnsAcceptedOrder()
    {
        var courier = await CreateApprovedCourier();
        var order = await CreateSubmittedOrder();

        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = courier.CourierId,
            OrderId = order.OrderId
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(OrderStatus.Accepted));
        Assert.That(result.CourierId, Is.EqualTo(courier.CourierId));
    }

    [Test]
    // Empty CourierId should fail validation
    public void AcceptOrder_EmptyCourierId_ReturnsError()
    {
        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = Guid.Empty,
            OrderId = Guid.NewGuid()
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "CourierId"), Is.True);
    }

    [Test]
    // Empty OrderId should fail validation
    public void AcceptOrder_EmptyOrderId_ReturnsError()
    {
        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = Guid.NewGuid(),
            OrderId = Guid.Empty
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "OrderId"), Is.True);
    }

    [Test]
    // Courier that does not exist should throw KeyNotFoundException
    public async Task AcceptOrder_CourierNotFound_ThrowsException()
    {
        var order = await CreateSubmittedOrder();

        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = Guid.NewGuid(), // does not exist in DB
            OrderId = order.OrderId
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Courier not found"));
    }

    [Test]
    // Order that does not exist should throw KeyNotFoundException
    public async Task AcceptOrder_OrderNotFound_ThrowsException()
    {
        var courier = await CreateApprovedCourier();

        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = courier.CourierId,
            OrderId = Guid.NewGuid() // does not exist in DB
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Order not found"));
    }

    [Test]
    // Pending courier should not be allowed to accept orders
    public async Task AcceptOrder_PendingCourier_ThrowsException()
    {
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Pending Courier",
            Email = "pending@example.com",
            PasswordHash = "hashedpassword",
            Status = CourierStatus.Pending // not approved yet
        };
        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();

        var order = await CreateSubmittedOrder();

        var command = new AcceptOrder.AcceptOrderCommand
        {
            CourierId = courier.CourierId,
            OrderId = order.OrderId
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("not approved"));
    }

    [Test]
    // Order already accepted by another courier should not be accepted again
    public async Task AcceptOrder_AlreadyAcceptedOrder_ThrowsException()
    {
        var courier1 = await CreateApprovedCourier();

        var courier2 = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Second Courier",
            Email = "courier2@example.com",
            PasswordHash = "hashedpassword",
            Status = CourierStatus.Approved
        };
        _context.Couriers.Add(courier2);
        await _context.SaveChangesAsync();

        var order = await CreateSubmittedOrder();

        // First courier accepts the order
        await _mediator.Send(new AcceptOrder.AcceptOrderCommand
        {
            CourierId = courier1.CourierId,
            OrderId = order.OrderId
        });

        // Second courier tries to accept the same order
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(new AcceptOrder.AcceptOrderCommand
            {
                CourierId = courier2.CourierId,
                OrderId = order.OrderId
            }));

        Assert.That(ex!.Message, Does.Contain("not available"));
    }
}