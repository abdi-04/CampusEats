using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using CampusEatsv2.Infrastructure.Services.OrderServices;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ConfirmDeliveryCustomerTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddValidatorsFromAssemblyContaining<
            ConfirmDeliveryCustomer.Validator>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(ConfirmDeliveryCustomer.ConfirmDeliveryCustomerHandler).Assembly);
        });

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

    // --------------------------
    // Helpers
    // --------------------------

    private async Task<Order> CreatePickedUpOrderAsync(Guid customerId)
    {
        var customer = new Customer
        {
            CustomerId = customerId,
            FullName = "Test Customer",
            Email = "customer@test.com",
            PasswordHash = "hash"
        };

        _context.Customers.Add(customer);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.PickedUp,
            DeliveryFee = 50,
            Tip = 0,
            CreationTime = DateTime.UtcNow,
            PickupAddress = "Test address"
        };

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();
        return order;
    }

    // --------------------------
    // Tests
    // --------------------------

    [Test]
    public async Task ConfirmDeliveryCustomer_SuccessfullyConfirmsDelivery_AndStoresTip()
    {
        var customerId = Guid.NewGuid();
        var order = await CreatePickedUpOrderAsync(customerId);

        var command = new ConfirmDeliveryCustomer.ConfirmDeliveryCustomerCommand
        {
            OrderId = order.OrderId,
            CustomerId = customerId,
            Tip = 15m
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Status, Is.EqualTo(OrderStatus.Delivered));
        Assert.That(result.Tip, Is.EqualTo(15m));
        Assert.That(result.DeliveryFee, Is.EqualTo(50m));
    }

    [Test]
    public void ConfirmDeliveryCustomer_OrderDoesNotExist_ThrowsException()
    {
        var command = new ConfirmDeliveryCustomer.ConfirmDeliveryCustomerCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Tip = 10m
        };

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _mediator.Send(command));
    }

    [Test]
    public async Task ConfirmDeliveryCustomer_WhenOrderNotPickedUp_ThrowsException()
    {
        var customerId = Guid.NewGuid();

        var customer = new Customer
        {
            CustomerId = customerId,
            FullName = "Test Customer",
            Email = "customer@test.com",
            PasswordHash = "hash"
        };

        _context.Customers.Add(customer);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Submitted,
            DeliveryFee = 50,
            CreationTime = DateTime.UtcNow,
            PickupAddress = "Test address"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var command = new ConfirmDeliveryCustomer.ConfirmDeliveryCustomerCommand
        {
            OrderId = order.OrderId,
            CustomerId = customerId,
            Tip = 10m
        };

        Assert.ThrowsAsync<ArgumentException>(() =>
            _mediator.Send(command));
    }
}
