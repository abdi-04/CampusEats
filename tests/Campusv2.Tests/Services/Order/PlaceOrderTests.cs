using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.OrderServices;
using CampusEatsv2.Web;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class PlaceOrderTests
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

    private async Task<(Guid customerId, Guid cartId, Product product)> SeedCartAsync(int quantity = 2)
    {
        var customerId = Guid.NewGuid();

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Title = "Checkout Burger",
            Description = "Burger ready for checkout",
            Price = 55m,
            ImageUrl = "https://example.com/checkout-burger.jpg"
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
                    CartId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    Price = product.Price,
                    Product = product
                }
            ]
        };

        _context.Products.Add(product);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return (customerId, cart.CartId, product);
    }

    [Test]
    public async Task PlaceOrderService_ValidData_ReturnsResult()
    {
        var (customerId, _, _) = await SeedCartAsync();
        var orderId = Guid.NewGuid();

        var command = new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = orderId,
            CustomerId = customerId,
            DeliveryFee = 20,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        };

        var result = await _mediator.Send(command);

        Assert.That(result.OrderId, Is.EqualTo(orderId));
        Assert.That(result.CustomerId, Is.EqualTo(customerId));
        Assert.That(result.DeliveryFee, Is.EqualTo(20));
        Assert.That(result.Tip, Is.EqualTo(0));
        Assert.That(result.Status, Is.EqualTo(OrderStatus.Submitted));
        Assert.That(result.Payment, Is.Not.Null);
        Assert.That(result.Payment!.Method, Is.EqualTo(PaymentMethods.Vipps));
        Assert.That(result.OrderItems, Has.Count.EqualTo(1));
        Assert.That(result.PickupAddress, Is.EqualTo("123 Test St"));
    }

    [Test]
    public void PlaceOrderService_NegativeDeliveryFee_ReturnsValidationError()
    {
        var command = new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            DeliveryFee = -2,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "DeliveryFee"), Is.True);
    }

    [Test]
    public void PlaceOrderService_CreateOrderWithoutCartItems_ThrowsException()
    {
        var command = new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            DeliveryFee = 20,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task PlaceOrderService_WhenCartExists_MovesCartItemsIntoOrder()
    {
        var (customerId, cartId, product) = await SeedCartAsync(quantity: 2);

        var order = await _mediator.Send(new PlaceOrderService.PlaceOrderCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerId = customerId,
            DeliveryFee = 20,
            Payment = PaymentMethods.Vipps,
            PickupAddress = "123 Test St"
        });

        var remainingCartItems = await _context.CartItems
            .Where(item => item.CartId == cartId)
            .ToListAsync();

        Assert.That(remainingCartItems, Is.Empty);
        Assert.That(order.OrderItems, Has.Count.EqualTo(1));
        Assert.That(order.OrderItems[0].ProductId, Is.EqualTo(product.ProductId));
    }
}
