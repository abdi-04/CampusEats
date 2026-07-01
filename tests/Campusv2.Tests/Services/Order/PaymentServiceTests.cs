using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services;
using CampusEatsv2.Infrastructure.Services.OrderServices;
using CampusEatsv2.Web;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class PaymentServiceTests
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

    private async Task<Guid> SeedCartAsync()
    {
        var customerId = Guid.NewGuid();

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Title = "Payment Burger",
            Description = "Burger used for payment tests",
            Price = 10m,
            ImageUrl = "https://example.com/payment-burger.jpg"
        };

        var cart = new Cart
        {
            CartId = Guid.NewGuid(),
            UserId = customerId,
            Items =
            [
                new CartItem
                {
                    CartItemId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Quantity = 2,
                    Price = product.Price,
                    Product = product
                }
            ]
        };

        _context.Products.Add(product);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return customerId;
    }

    [Test]
    public async Task PaymentService_ValidData_ReturnsResult()
    {
        var customerId = await SeedCartAsync();
        var orderId = Guid.NewGuid();

        // Place order first
        var order = await _mediator.Send(new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = orderId,
            CustomerId = customerId,
            DeliveryFee = 20,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        });

        // Process payment with correct amount
        var result = await _mediator.Send(new PaymentService.ProcessPaymentCommand
        {
            OrderId = order.OrderId,
            PaymentMethod = PaymentMethods.Vipps,
            Amount = order.TotalAmount
        });

        Assert.That(result, Is.True);

        var updatedOrder = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder!.Payment, Is.Not.Null);
        Assert.That(updatedOrder.Payment!.Status, Is.EqualTo(PaymentStatus.Paid));
        Assert.That(updatedOrder.Payment.Method, Is.EqualTo(PaymentMethods.Vipps));
        Assert.That(updatedOrder.Status, Is.EqualTo(OrderStatus.Submitted));
    }

    [Test]
    public async Task PaymentService_InvalidOrderId_ReturnsFalse()
    {
        var result = await _mediator.Send(new PaymentService.ProcessPaymentCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentMethod = PaymentMethods.Vipps,
            Amount = 1m
        });

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task PaymentService_InvalidAmount_ReturnsFalse()
    {
        var customerId = await SeedCartAsync();
        var orderId = Guid.NewGuid();

        var order = await _mediator.Send(new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = orderId,
            CustomerId = customerId,
            DeliveryFee = 20,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        });

        // Wrong amount
        var result = await _mediator.Send(new PaymentService.ProcessPaymentCommand
        {
            OrderId = order.OrderId,
            PaymentMethod = PaymentMethods.Vipps,
            Amount = 1m
        });

        Assert.That(result, Is.False);
    }

    [Test]
    public void PaymentService_InvalidPaymentMethod_ThrowsValidationException()
    {
        var command = new PaymentService.ProcessPaymentCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentMethod = (PaymentMethods)999,
            Amount = 1m
        };

        Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));
    }
}