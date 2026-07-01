using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CartServices.RemoveCartItem;

namespace CampusEatsv2.Tests;

[TestFixture]
public class RemoveCartItemTests
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

    private async Task<(Guid customerId, Product product)> SeedCartWithItemAsync(int quantity)
    {
        var customerId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Title = "Loaded Fries",
            Description = "Fries with toppings",
            Price = 45m,
            ImageUrl = "https://example.com/fries.jpg"
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

        return (customerId, product);
    }

    [Test]
    public async Task RemoveCartItem_WhenRemovingLessThanQuantity_DecreasesQuantity()
    {
        var (customerId, product) = await SeedCartWithItemAsync(quantity: 3);

        var result = await _mediator.Send(new RemoveCartItemCommand
        {
            CustomerId = customerId,
            ProductId = product.ProductId,
            Quantity = 1
        });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task RemoveCartItem_WhenRemovingEntireQuantity_RemovesItemFromCart()
    {
        var (customerId, product) = await SeedCartWithItemAsync(quantity: 2);

        var result = await _mediator.Send(new RemoveCartItemCommand
        {
            CustomerId = customerId,
            ProductId = product.ProductId,
            Quantity = 2
        });

        Assert.That(result.Items, Is.Empty);
    }
}
