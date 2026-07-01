using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CartServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ShowCustomerCartTests
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

    [Test]
    public async Task ShowCustomerCart_WhenCartDoesNotExist_ReturnsEmptyCartModel()
    {
        var customerId = Guid.NewGuid();

        var result = await _mediator.Send(new ShowCustomerCart.ShowCustomerCartQuery
        {
            CustomerId = customerId
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(customerId));
        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task ShowCustomerCart_WhenCartExists_ReturnsItemsWithProductDetails()
    {
        var customerId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Title = "Spicy Wrap",
            Description = "Grilled chicken wrap",
            Price = 79m,
            ImageUrl = "https://example.com/wrap.jpg"
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
        _context.ChangeTracker.Clear();

        var result = await _mediator.Send(new ShowCustomerCart.ShowCustomerCartQuery
        {
            CustomerId = customerId
        });

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Quantity, Is.EqualTo(2));
        Assert.That(result.Items[0].Product, Is.Not.Null);
        Assert.That(result.Items[0].Product!.Title, Is.EqualTo("Spicy Wrap"));
    }
}
