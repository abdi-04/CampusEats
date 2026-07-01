using CampusEatsv2.Core.Models;
using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using CampusEatsv2.Infrastructure.Services.CourierServices;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class PickupOrderTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Required infrastructure for handlers
        services.AddLogging();

        // In-memory EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Validators
        services.AddValidatorsFromAssemblyContaining<
            PickupOrder.Validator>();

        // MediatR (commands + domain events)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(PickupOrder.PickupOrderHandler).Assembly);
        });

        // Validation pipeline
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));

        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddSingleton<INotificationSender, FakeNotificationSender>();
        services.AddSingleton<INotificationStore, FakeNotificationStore>();
        
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

    // Helpers

    private async Task<Order> CreateAcceptedOrderAsync(Guid courierId)
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
            CourierId = courierId,
            CustomerId = customerId,
            Status = OrderStatus.Accepted,
            CreationTime = DateTime.UtcNow,
            PickupAddress = "Test pickup",
            DeliveryFee = 40,
            Tip = 10
        };
    
        _context.Orders.Add(order);
    
        await _context.SaveChangesAsync();
        return order;
    }


    // Tests

    [Test]
    public async Task PickupOrder_ValidData_ReturnsPickedUpOrder()
    {
        var courierId = Guid.NewGuid();
        var order = await CreateAcceptedOrderAsync(courierId);

        var command = new PickupOrder.PickupOrderCommand
        {
            OrderId = order.OrderId,
            CourierId = courierId
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Status, Is.EqualTo(OrderStatus.PickedUp));
    }

    [Test]
    public void PickupOrder_OrderDoesNotExist_ThrowsException()
    {
        var command = new PickupOrder.PickupOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CourierId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _mediator.Send(command));
    }

    [Test]
    public async Task PickupOrder_WhenNotAccepted_ThrowsException()
    {
        var courierId = Guid.NewGuid();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CourierId = courierId,
            Status = OrderStatus.Delivered,
            PickupAddress = "James stree 31"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var command = new PickupOrder.PickupOrderCommand
        {
            OrderId = order.OrderId,
            CourierId = courierId
        };

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(command));
    }
}