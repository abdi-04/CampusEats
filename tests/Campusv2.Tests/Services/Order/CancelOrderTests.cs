using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.OrderServices;
using CampusEatsv2.Web;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class CancelOrderTests
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

    private async Task<Order> CreateOrder(OrderStatus status = OrderStatus.Submitted)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CourierId = Guid.NewGuid(),
            Status = status,
            CreationTime = DateTime.UtcNow,
            PickupAddress = "123 Test St",
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    [Test]
    public async Task CancelOrder_SuccessfullyCancelsOrder()
    {
        // Arrange
        var order = await CreateOrder(OrderStatus.Submitted);

        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.That(result.Status, Is.EqualTo(OrderStatus.Cancelled));

        var updatedOrder = await _context.Orders.FindAsync(order.OrderId);
        Assert.That(updatedOrder!.Status, Is.EqualTo(OrderStatus.Cancelled));
    }

    [Test]
    public void CancelOrder_EmptyOrderId_ThrowsValidationException()
    {
        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = Guid.Empty,
            CustomerId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public void CancelOrder_EmptyCustomerId_ThrowsValidationException()
    {
        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.Empty
        };

        Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public void CancelOrder_NonExistentOrder_ThrowsKeyNotFoundException()
    {
        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task CancelOrder_WrongCustomer_ThrowsKeyNotFoundException()
    {
        var order = await CreateOrder();

        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = order.OrderId,
            CustomerId = Guid.NewGuid() // different customer
        };

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task CancelOrder_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        var order = await CreateOrder(OrderStatus.Cancelled);

        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task CancelOrder_AlreadyDelivered_ThrowsInvalidOperationException()
    {
        var order = await CreateOrder(OrderStatus.Delivered);

        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task CancelOrder_AlreadyPickedUp_ThrowsInvalidOperationException()
    {
        var order = await CreateOrder(OrderStatus.PickedUp);

        var command = new CancelOrder.CancelOrderCommand
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));
    }
}
