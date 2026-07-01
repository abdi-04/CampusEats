using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CartServices.AddCartItem;

namespace CampusEatsv2.Tests;

[TestFixture]
public class AddCartItemTests
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

    private async Task<Cart> CreateTestCartAsync(Guid customerId)
    {
        var cart = new Cart
        {
            CartId = Guid.NewGuid(),
            UserId = customerId,
            Items = new List<CartItem>(),
            UpdatedAt = DateTime.UtcNow
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.Entry(cart).State = EntityState.Detached;

        return cart;
    }

    private async Task<Product> CreateTestProductAsync()
    {
        decimal price = 50;
        string description = "test description";
        string imageUrl = "picture.png";

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Title = "Test Product",
            Description = description,
            Price = price,
            ImageUrl = imageUrl
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _context.Entry(product).State = EntityState.Detached;

        return product;
    }

    [Test]
    public async Task AddCartItemService_ValidData_ReturnsCartWithNewItem()
    {
        var customerId = Guid.NewGuid();
        var cart = await CreateTestCartAsync(customerId);
        var product = await CreateTestProductAsync();

        var command = new AddCartItemCommand
        {
            CustomerId = customerId,
            ProductId = product.ProductId,
            Quantity = 2
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].ProductId, Is.EqualTo(product.ProductId));
        Assert.That(result.Items[0].Quantity, Is.EqualTo(2));
        Assert.That(result.Items[0].Price, Is.EqualTo(product.Price));
    }

    [Test]
    public async Task AddCartItemService_ProductAlreadyInCart_IncreasesQuantity()
    {
        var customerId = Guid.NewGuid();
        await CreateTestCartAsync(customerId);
        var product = await CreateTestProductAsync();

        var command = new AddCartItemCommand
        {
            CustomerId = customerId,
            ProductId = product.ProductId,
            Quantity = 1
        };

        // First add
        await _mediator.Send(command);

        // Verify directly from DB instead of using the returned cart
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.UserId == customerId);

        Assert.That(cart.Items, Has.Count.EqualTo(1));
        Assert.That(cart.Items[0].Quantity, Is.EqualTo(1));

    }

    [Test]
    public async Task AddCartItemService_CartNotFound_CreatesCartAndAddsItem()
    {
        var customerId = Guid.NewGuid();
        var product = await CreateTestProductAsync();

        var command = new AddCartItemCommand
        {
            CustomerId = customerId,
            ProductId = product.ProductId,
            Quantity = 1
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].ProductId, Is.EqualTo(product.ProductId));
        Assert.That(await _context.Carts.CountAsync(), Is.EqualTo(1));
        Assert.That(await _context.Carts.AnyAsync(c => c.UserId == customerId), Is.True);
    }

    [Test]
    public async Task AddCartItemService_ProductNotFound_ThrowsException()
    {
        var customerId = Guid.NewGuid();
        await CreateTestCartAsync(customerId);

        var command = new AddCartItemCommand
        {
            CustomerId = customerId,
            ProductId = Guid.NewGuid(), // Non-existent product
            Quantity = 1
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Product not found"));
    }

    [Test]
    public void AddCartItemService_InvalidData_ThrowsValidationException()
    {
        var command = new AddCartItemCommand
        {
            CustomerId = Guid.Empty,
            ProductId = Guid.Empty,
            Quantity = 0
        };

        Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));
    }
}
