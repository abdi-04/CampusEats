using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ManageProductsAdminTests
{
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var provider = Program.ConfigureServices(useInMemory: true);
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

    [Test]
    public async Task CreateProduct_ValidInput_AddsProduct()
    {
        var command = new ManageProductsAdmin.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Burger",
            Price = 9.90m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/burger.jpg",
            Description = "Beef burger"
        };

        var result = await _mediator.Send(command);

        Assert.That(result.Title, Is.EqualTo("Burger"));
        Assert.That(result.DeliveryFee, Is.EqualTo(2.50m));
        Assert.That(await _context.Products.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void CreateProduct_DuplicateTitle_ThrowsInvalidOperation()
    {
        var first = new ManageProductsAdmin.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Burger",
            Price = 9.90m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/burger.jpg",
            Description = "Beef burger"
        };

        var duplicate = new ManageProductsAdmin.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Burger",
            Price = 10.90m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/burger-2.jpg",
            Description = "Duplicate title"
        };

        Assert.DoesNotThrowAsync(async () => await _mediator.Send(first));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _mediator.Send(duplicate));
    }

    [Test]
    public async Task UpdateProduct_ValidInput_UpdatesProduct()
    {
        var created = await _mediator.Send(new ManageProductsAdmin.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Burger",
            Price = 9.90m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/burger.jpg",
            Description = "Beef burger"
        });

        var updated = await _mediator.Send(new ManageProductsAdmin.UpdateProductCommand
        {
            ProductId = created.ProductId,
            Title = "Chicken Burger",
            Price = 11.50m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/chicken.jpg",
            Description = "Chicken burger"
        });

        Assert.That(updated.Title, Is.EqualTo("Chicken Burger"));
        Assert.That(updated.Price, Is.EqualTo(11.50m));
        Assert.That(updated.DeliveryFee, Is.EqualTo(2.50m));
    }

    [Test]
    public async Task DeleteProduct_ExistingProduct_RemovesProduct()
    {
        var created = await _mediator.Send(new ManageProductsAdmin.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Burger",
            Price = 9.90m,
            DeliveryFee = 2.50m,
            ImageUrl = "https://example.com/burger.jpg",
            Description = "Beef burger"
        });

        await _mediator.Send(new ManageProductsAdmin.DeleteProductCommand
        {
            ProductId = created.ProductId
        });

        Assert.That(await _context.Products.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public void DeleteProduct_NonExistingProduct_ThrowsKeyNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(new ManageProductsAdmin.DeleteProductCommand
            {
                ProductId = nonExistingId
            }));
    }
}
